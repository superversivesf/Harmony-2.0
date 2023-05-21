using System.Text.Json;
using Harmony.Dto;
using Harmony.Dto.AudioChapters;
using Harmony.Dto.AudioFormat;
using Harmony.Dto.AudioStream;
using TagLib;
using Xabe.FFmpeg;
using AudioStream = Harmony.Dto.AudioStream.AudioStream;
using File = System.IO.File;

namespace Harmony;

internal class AaxAudioConvertor
{
    private readonly string _activationBytes;
    private readonly int _bitrate;
    private readonly string _inputFolder;
    private readonly string _outputFolder;
    private readonly bool _quietMode;
    private readonly string _storageFolder;
    private readonly string _workingFolder;

    public AaxAudioConvertor(string activationBytes, int bitrate, bool quietMode, string inputFolder,
        string outputFolder, string storageFolder, string workingFolder)
    {
        _activationBytes = activationBytes;
        _bitrate = bitrate;
        _quietMode = quietMode;
        _inputFolder = inputFolder;
        _outputFolder = outputFolder;
        _storageFolder = storageFolder;
        _workingFolder = workingFolder;
    }

    internal void Execute()
    {
        var logger = new Logger(_quietMode);
        logger.WriteLine("\bDone");

        logger.Write("Checking folders and purging working files ... ");
        CheckFolders();
        logger.WriteLine("Done");
        var filePaths = Directory.GetFiles(_inputFolder, "*.aax").ToList();
        logger.WriteLine($"Found {filePaths.Count} aax files to process\n");
        ProcessAaxFiles(filePaths);
    }

    private void ProcessAaxFiles(List<string> filePaths)
    {
        foreach (var f in filePaths) ProcessAaxFile(f);
    }

    private void ProcessAaxFile(string f)
    {
        var logger = new Logger(_quietMode);
        var storageFolder = _storageFolder;

        var aaxInfo = GetAaxInfo(f);

        // Write out relevant stats
        logger.WriteLine($"Title: {CleanTitle(aaxInfo.format.format?.tags?.title)}");
        logger.WriteLine($"Author(s): {aaxInfo.format.format?.tags?.artist}");

        var duration = double.Parse(aaxInfo.format.format?.duration!);
        var h = (int)duration / 3600;
        var m = ((int)duration - h * 3600) / 60;
        var s = (int)duration - h * 3600 - m * 60;

        logger.WriteLine($"Length: {h:D2}:{m:D2}:{s:D2}");
        logger.WriteLine($"Chapters: {aaxInfo.chapters.chapters.Count}");

        var intermediateFile = ProcessToMp3(f);
        var coverFile = GenerateCover(f);
        var outputDirectory = ProcessChapters(intermediateFile, aaxInfo, coverFile);

        logger.Write("Moving Cover file ... ");
        var coverFileDestination = Path.Combine(outputDirectory, "Cover.jpg");
        File.Move(coverFile, coverFileDestination);
        logger.WriteLine("Done");

        logger.Write("Moving AAX file to storage ... ");
        var storageFile = Path.Combine(storageFolder, Path.GetFileName(f));
        File.Move(f, storageFile);
        logger.WriteLine("Done");

        // Cleanup 
        logger.Write("Cleaning up intermediate files ... ");
        File.Delete(intermediateFile);
        logger.WriteLine("Done\n");

        // https://github.com/inAudible-NG/tables
    }

    private void PurgeOutputDirectory(string outputDirectory)
    {
        if (Directory.Exists(outputDirectory))
        {
            var di = new DirectoryInfo(outputDirectory);

            foreach (var file in di.GetFiles()) file.Delete();
        }
    }

    private string GenerateCover(string f)
    {
        var logger = new Logger(_quietMode);
        var filePath = f;
        var activationBytes = _activationBytes;

        var coverFile = Path.Combine(_workingFolder, "Cover.jpg");

        logger.Write("Writing Cover File ... ");

        var ffmpeg = FFmpeg.Conversions.New()
            .AddParameter(
                $"-activation_bytes {activationBytes} -i \"{filePath}\" -an -codec:v copy \"{coverFile}\"").Start();

        ffmpeg.Wait();

        logger.WriteLine("Done");

        return coverFile;
    }

    private string ProcessChapters(string filepath, AaxInfoDto aaxInfoDto, string coverPath)
    {
        var logger = new Logger(_quietMode);
        var outputFolder = _outputFolder;
        var filePath = filepath;
        var title = CleanTitle(aaxInfoDto.format.format?.tags?.title);
        var author = CleanAuthor(aaxInfoDto.format.format?.tags?.artist!);
        var outputDirectory = Path.Combine(outputFolder, author);
        outputDirectory = Path.Combine(outputDirectory, title ?? string.Empty);
        var m3UFileName = $"{title}.m3u";

        var invalidPathChars = Path.GetInvalidPathChars();
        foreach (var c in invalidPathChars) outputDirectory = outputDirectory.Replace(c, '_');

        PurgeOutputDirectory(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        var m3UFilePath = Path.Combine(outputDirectory, m3UFileName);
        var m3UFile = new StreamWriter(m3UFilePath);
        var chapterCount = aaxInfoDto.chapters.chapters.Count;
        string formatString;

        if (chapterCount > 100)
            formatString = "D3";
        else if (chapterCount > 10)
            formatString = "D2";
        else
            formatString = "D1";

        logger.WriteLine($"Processing {title} with {chapterCount} Chapters");

        InitM3U(m3UFile);

        foreach (var c in aaxInfoDto.chapters.chapters)
        {
            var startChapter = c.start_time;
            var endChapter = c.end_time;
            var chapterNumber = c.id + 1; // zero based
            var chapterFileTitle = c.tags?.title?.Trim();
            var chapterFile = title + "-" + chapterNumber.ToString(formatString) + "-" + chapterFileTitle + ".mp3";
            var chapterFilePath = Path.Combine(outputDirectory, chapterFile);
            logger.Write($"\rWriting Chapter {c.id + 1} ...  ");

            var ffmpeg = FFmpeg.Conversions.New()
                .AddParameter(
                    $" -i \"{filePath}\" -ss \"{startChapter}\" -to \"{endChapter}\" -acodec mp3 \"{chapterFilePath}\""
                )
                .Start();

            while (!ffmpeg.IsCompleted)
            {
                logger.AdvanceSpinner();
                Thread.Sleep(100);
            }

            // Encode MP3 tags and cover here // write m3u file as well at the same time

            UpdateM3UAndTagFile(m3UFile, chapterFilePath, aaxInfoDto, coverPath, c);

            logger.WriteLine("\bDone");
        }

        m3UFile.Close();
        return outputDirectory;
    }

    private void InitM3U(StreamWriter m3UFile)
    {
        // ReSharper disable once StringLiteralTypo
        m3UFile.WriteLine("# EXTM3U");
    }

    private void UpdateM3UAndTagFile(StreamWriter m3UFile, string chapterFile, AaxInfoDto aaxInfoDto, string coverPath,
        Chapter chapter)
    {
        var tagFile = TagLib.File.Create(chapterFile);
        var title = CleanTitle(aaxInfoDto.format.format?.tags?.title);
        m3UFile.WriteLine(
            $"# EXTINF:{tagFile.Properties.Duration.TotalSeconds:0F},{aaxInfoDto.format.format?.tags?.title} - {chapter.tags?.title}");
        m3UFile.WriteLine(Path.GetFileName(chapterFile));

        var coverPicture = new PictureLazy(coverPath);
        tagFile.Tag.Pictures = new IPicture[] { coverPicture };

        tagFile.Tag.Title = title + " - " + chapter.tags?.title;
        tagFile.Tag.AlbumArtists = new[] { aaxInfoDto.format.format?.tags?.artist };
        tagFile.Tag.Album = title;
        tagFile.Tag.Track = (uint)chapter.id + 1;
        tagFile.Tag.TrackCount = (uint)aaxInfoDto.chapters.chapters.Count;

        tagFile.Tag.Copyright = aaxInfoDto.format.format?.tags?.copyright;
        tagFile.Tag.DateTagged = aaxInfoDto.format.format?.tags?.creation_time;
        tagFile.Tag.Comment = aaxInfoDto.format.format?.tags?.comment;
        tagFile.Tag.Description = aaxInfoDto.format.format?.tags?.comment;
        tagFile.Tag.Genres = new[] { aaxInfoDto.format.format?.tags?.genre };
        tagFile.Tag.Publisher = "";
        if (aaxInfoDto.format.format != null) tagFile.Tag.Year = (uint)aaxInfoDto.format.format.tags!.creation_time.Year;

        tagFile.Save();
    }

    private string CleanAuthor(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";

        var authors = name.Split(',');
        if (authors.Count() > 4) return "Various";

        return name.Replace("Jr.", "Jr").Trim();
    }

    private string? CleanTitle(string? title)
    {
        return title?.Replace("(Unabridged)", string.Empty).Replace(":", " -").Replace("'", string.Empty)
            .Replace("?", string.Empty).Trim();
    }

    private string ProcessToMp3(string filePath)
    {
        var logger = new Logger(_quietMode);
        var activationBytes = _activationBytes;
        var bitrate = _bitrate;

        var intermediateFile = Path.GetFileName(filePath);
        intermediateFile = Path.ChangeExtension(intermediateFile, "mp3");
        intermediateFile = Path.Combine(_workingFolder, intermediateFile);

        logger.Write("Recoding to mp3 ...  ");

        var ffmpeg = FFmpeg.Conversions.New()
            .AddParameter(
                $"-activation_bytes {activationBytes} -i \"{filePath}\" -vn -codec:a mp3 -ab {bitrate}k \"{intermediateFile}\""
            )
            .Start();

        while (!ffmpeg.IsCompleted)
        {
            logger.AdvanceSpinner();
            Thread.Sleep(100);
        }

        logger.WriteLine("\bDone");

        return intermediateFile;
    }

    private AaxInfoDto GetAaxInfo(string f)
    {
        var logger = new Logger(_quietMode);

        var filePath = f;

        logger.Write("Probing ");

        var task = Probe.New().Start(
            $"-print_format json -loglevel quiet -activation_bytes {_activationBytes} -show_format \"{filePath}\""
        );
        task.Wait();
        var formatJson = task.Result;

        logger.Write(".");

        task = Probe.New().Start(
            $"-print_format json -loglevel quiet -activation_bytes {_activationBytes} -show_streams \"{filePath}\""
        );
        task.Wait();
        var streamsJson = task.Result;

        logger.Write(".");

        task = Probe.New().Start(
            $"-print_format json -loglevel quiet -activation_bytes {_activationBytes} -show_chapters \"{filePath}\""
        );
        task.Wait();

        var chaptersJson = task.Result;

        logger.Write(".");

        var audioFormat = JsonSerializer.Deserialize<AudioFormat>(formatJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var audioChapters = JsonSerializer.Deserialize<AudioChapters>(chaptersJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var audioStreams = JsonSerializer.Deserialize<AudioStream>(streamsJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var result = new AaxInfoDto(audioFormat!, audioChapters!, audioStreams!);

        logger.WriteLine(" Done");

        return result;
    }

    private void CheckFolders()
    {
        if (!Directory.Exists(_inputFolder)) throw new Exception("Input folder does not exist: " + _inputFolder);

        if (!Directory.Exists(_outputFolder)) throw new Exception("Output folder does not exist: " + _inputFolder);

        if (!Directory.Exists(_storageFolder)) throw new Exception("Storage folder does not exist: " + _inputFolder);

        if (!Directory.Exists(_workingFolder)) throw new Exception("Working folder does not exist: " + _inputFolder);

        var di = new DirectoryInfo(_workingFolder);

        foreach (var file in di.GetFiles()) file.Delete();
    }
}