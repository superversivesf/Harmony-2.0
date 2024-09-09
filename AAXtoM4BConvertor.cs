using System.Text.Json;
using Harmony.Dto;
using TagLib;
using TagLib.Id3v2;
using Xabe.FFmpeg;
using AudioStream = Harmony.Dto.AudioStreamDto;
using File = System.IO.File;

namespace Harmony;

internal class AaxToM4BConvertor
{
    private readonly string _activationBytes;
    private readonly int _bitrate;
    private readonly string _inputFolder;
    private readonly string _outputFolder;
    private readonly bool _quietMode;
    private readonly string _storageFolder;
    private readonly string _workingFolder;

    public AaxToM4BConvertor(string activationBytes, int bitrate, bool quietMode, string inputFolder,
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
        logger.WriteLine($"Title: {CleanTitle(aaxInfo.Format.format?.tags?.title)}");
        logger.WriteLine($"Author(s): {aaxInfo.Format.format?.tags?.artist}");

        if (aaxInfo.Format.format?.duration != null)
        {
            var duration = double.Parse(aaxInfo.Format.format.duration);
            var h = (int)duration / 3600;
            var m = ((int)duration - h * 3600) / 60;
            var s = (int)duration - h * 3600 - m * 60;

            logger.WriteLine($"Length: {h:D2}:{m:D2}:{s:D2}");
        }
        else
        {
            logger.WriteLine($"Length: 0! Something is wrong");
        }

        logger.WriteLine($"Chapters: {aaxInfo.Chapters.chapters?.Count}");

        var intermediateFile = ProcessToM4a(f);
        var coverFile = GenerateCover(f);
        var outputDirectory = PrepareOutputDirectory(aaxInfo);

        CreateM4bFile(intermediateFile, aaxInfo, coverFile, outputDirectory);

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
    }

    private string ProcessToM4a(string filePath)
    {
        var logger = new Logger(_quietMode);
        var activationBytes = _activationBytes;

        var intermediateFile = Path.GetFileName(filePath);
        intermediateFile = Path.ChangeExtension(intermediateFile, "m4a");
        intermediateFile = Path.Combine(_workingFolder, intermediateFile);

        logger.Write("Decrypting to M4A ... ");

        var ffmpeg = FFmpeg.Conversions.New()
            .AddParameter(
                $"-activation_bytes {activationBytes} -i \"{filePath}\" -c:a copy -vn \"{intermediateFile}\""
            )
            .Start();

        ffmpeg.Wait();

        logger.WriteLine("Done");

        return intermediateFile;
    }

    private void CreateM4bFile(string intermediateFile, AaxInfoDto aaxInfo, string coverFile, string outputDirectory)
    {
        var logger = new Logger(_quietMode);
        var title = CleanTitle(aaxInfo.Format.format?.tags?.title);
        var m4bFilePath = Path.Combine(outputDirectory, $"{title}.m4b");

        logger.Write("Creating M4B file... ");

        var chapterMetadataFile = CreateChapterMetadataFile(aaxInfo.Chapters.chapters);

        var ffmpegArgs = $"-i \"{intermediateFile}\" -i \"{coverFile}\" -i \"{chapterMetadataFile}\" " +
                         $"-map 0:a -map 1 -map_metadata 2 " +
                         $"-c:a aac -b:a {_bitrate}k -c:v copy " +
                         $"-metadata title=\"{title}\" " +
                         $"-metadata artist=\"{aaxInfo.Format.format?.tags?.artist}\" " +
                         $"-metadata album=\"{title}\" " +
                         $"-metadata date=\"{aaxInfo.Format.format?.tags?.creationTime.Year}\" " +
                         $"-metadata genre=\"{aaxInfo.Format.format?.tags?.genre}\" " +
                         $"-metadata copyright=\"{aaxInfo.Format.format?.tags?.copyright}\" " +
                         $"-metadata comment=\"{aaxInfo.Format.format?.tags?.comment}\" " +
                         $"\"{m4bFilePath}\"";

        var ffmpeg = FFmpeg.Conversions.New().AddParameter(ffmpegArgs).Start();
        ffmpeg.Wait();

        File.Delete(chapterMetadataFile);

        logger.WriteLine("Done");
    }

    private string CreateChapterMetadataFile(List<ChapterDto>? chapters)
    {
        var metadataFilePath = Path.Combine(_workingFolder, "chapters.txt");
        using var writer = new StreamWriter(metadataFilePath);
        writer.WriteLine(";FFMETADATA1");
        
        if (chapters != null)
        {
            foreach (var chapter in chapters)
            {
                writer.WriteLine("[CHAPTER]");
                writer.WriteLine($"TIMEBASE=1/1000");
                writer.WriteLine($"START={(long)(chapter.start * 1000)}");
                writer.WriteLine($"END={(long)(chapter.end * 1000)}");
                writer.WriteLine($"title={chapter.tags?.title}");
            }
        }

        return metadataFilePath;
    }

    private string PrepareOutputDirectory(AaxInfoDto aaxInfo)
    {
        var title = CleanTitle(aaxInfo.Format.format?.tags?.title);
        var author = CleanAuthor(aaxInfo.Format.format?.tags?.artist!);
        var outputDirectory = Path.Combine(_outputFolder, author);
        outputDirectory = Path.Combine(outputDirectory, title ?? string.Empty);
        var invalidPathChars = Path.GetInvalidPathChars();
        foreach (var c in invalidPathChars) outputDirectory = outputDirectory.Replace(c, '_');

        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    // The following methods remain the same as in AaxAudioConvertor
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

        var audioFormat = JsonSerializer.Deserialize<AudioFormatDto>(formatJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var audioChapters = JsonSerializer.Deserialize<AudioChaptersDto>(chaptersJson, new JsonSerializerOptions
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