﻿using System.Text.Json;
using Harmony.Dto;
using TagLib;
using TagLib.Id3v2;
using Xabe.FFmpeg;
using AudioStream = Harmony.Dto.AudioStreamDto;
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
    private readonly string _outputFormat;
    private readonly bool _keepMp3;

    public AaxAudioConvertor(string activationBytes, int bitrate, bool quietMode, string inputFolder,
        string outputFolder, string storageFolder, string workingFolder, string outputFormat, bool keepMp3)
    {
        _activationBytes = activationBytes;
        _bitrate = bitrate;
        _quietMode = quietMode;
        _inputFolder = inputFolder;
        _outputFolder = outputFolder;
        _storageFolder = storageFolder;
        _workingFolder = workingFolder;
        _outputFormat = outputFormat;
        _keepMp3 = keepMp3;
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

        logger.WriteLine($"Chapters: {aaxInfo.Chapters.chapters?.Length}");

        var intermediateFile = ProcessToMp3(f);
        var coverFile = GenerateCover(f);
        var outputDirectory = ProcessChapters(intermediateFile, aaxInfo, coverFile);

        if (_outputFormat == "m4b")
        {
            CreateM4bFile(outputDirectory, aaxInfo, coverFile);
            if (!_keepMp3)
            {
                DeleteMp3Files(outputDirectory);
            }
        }
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
    private void CreateM4bFile(string outputDirectory, AaxInfoDto aaxInfo, string coverFile)
    {
        var logger = new Logger(_quietMode);
        var title = CleanTitle(aaxInfo.Format.format?.tags?.title);
        var m4bFilePath = Path.Combine(outputDirectory, $"{title}.m4b");

        logger.Write("Creating M4B file... ");

        // Combine all MP3 files into a single M4B file
        var mp3Files = Directory.GetFiles(outputDirectory, "*.mp3").OrderBy(f => f).ToList();
        var ffmpegArgs = $"-i \"concat:{string.Join('|', mp3Files)}\" -i \"{coverFile}\" -map 0:a -map 1 " +
                         $"-c:a aac -b:a {_bitrate}k -c:v copy -f mp4 " +
                         $"-metadata title=\"{title}\" " +
                         $"-metadata artist=\"{aaxInfo.Format.format?.tags?.artist}\" ";

        // Add chapter metadata
        if (aaxInfo.Chapters.chapters != null)
        {
            double totalDuration = 0;
            int chapterCount = 0;
            foreach (var chapter in aaxInfo.Chapters.chapters)
            {
                ffmpegArgs += $"-metadata:s:a:0 \"chapter #{chapterCount++ + 1}={TimeSpan.FromSeconds(totalDuration):hh\\:mm\\:ss.fff}\" ";
                totalDuration += chapter.endTime - chapter.startTime;
            }
        }

        ffmpegArgs += $"\"{m4bFilePath}\"";

        var ffmpeg = FFmpeg.Conversions.New().AddParameter(ffmpegArgs).Start();
        ffmpeg.Wait();

        logger.WriteLine("Done");
    }
    private void CreateChapterMetadataFile(List<ChapterDto> chapters, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine(";FFMETADATA1");
        foreach (var chapter in chapters)
        {
            writer.WriteLine("[CHAPTER]");
            writer.WriteLine($"TIMEBASE=1/1000");
            writer.WriteLine($"START={(long)(chapter.start * 1000)}");
            writer.WriteLine($"END={(long)(chapter.end * 1000)}");
            writer.WriteLine($"title={chapter.tags?.title}");
        }
    }

    private void DeleteMp3Files(string outputDirectory)
    {
        foreach (var file in Directory.GetFiles(outputDirectory, "*.mp3"))
        {
            File.Delete(file);
        }
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
    var title = CleanTitle(aaxInfoDto.Format.format?.tags?.title);
    var author = CleanAuthor(aaxInfoDto.Format.format?.tags?.artist!);
    var outputDirectory = Path.Combine(outputFolder, author);
    outputDirectory = Path.Combine(outputDirectory, title ?? string.Empty);
    var invalidPathChars = Path.GetInvalidPathChars();
    foreach (var c in invalidPathChars) outputDirectory = outputDirectory.Replace(c, '_');

    PurgeOutputDirectory(outputDirectory);

    Directory.CreateDirectory(outputDirectory);
    var chapterCount = aaxInfoDto.Chapters.chapters?.Length ?? 0;
    string formatString = chapterCount > 100 ? "D3" : (chapterCount > 10 ? "D2" : "D1");

    logger.WriteLine($"Processing {title} with {chapterCount} Chapters");
    
    if (aaxInfoDto.Chapters.chapters != null)
    {
        for (int i = 0; i < chapterCount; i++)
        {
            var c = aaxInfoDto.Chapters.chapters[i];
            var startChapter = c.startTime;
            var endChapter = c.endTime;
            var chapterNumber = i + 1; // zero-based to one-based
            var chapterFileTitle = c.tags?.title?.Trim();
            var chapterFile = $"{title}-{chapterNumber.ToString(formatString)}.mp3";
            var chapterFilePath = Path.Combine(outputDirectory, chapterFile);
            logger.Write($"\rWriting Chapter {chapterNumber} ...  ");

            var ffmpeg = FFmpeg.Conversions.New()
                .AddParameter(
                    $" -i \"{filePath}\" -ss \"{startChapter}\" -to \"{endChapter}\" -acodec mp3 " +
                    $"-metadata track=\"{chapterNumber}/{chapterCount}\" " +
                    $"-metadata chapter=\"{chapterNumber}\" \"{chapterFilePath}\""
                )
                .Start();

            while (!ffmpeg.IsCompleted)
            {
                logger.AdvanceSpinner();
                Thread.Sleep(100);
            }

            //UpdateTagFile(chapterFilePath, aaxInfoDto, coverPath, c, chapterNumber, chapterCount);

            logger.WriteLine("\bDone");
        }
    }

    return outputDirectory;
}
    

private void UpdateTagFile(string chapterFile, AaxInfoDto aaxInfoDto, string coverPath,
    ChapterDto chapter, int chapterNumber, int totalChapters)
{
    var tagFile = TagLib.File.Create(chapterFile);
    var title = CleanTitle(aaxInfoDto.Format.format?.tags?.title);

    // Remove existing tags and create a new ID3v2 tag
    tagFile.RemoveTags(TagTypes.Id3v1);
    var id3v2Tag = tagFile.GetTag(TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;
    if (id3v2Tag == null)
    {
        throw new InvalidOperationException("Failed to create or retrieve ID3v2 tag.");
    }

    // Set standard tags
    id3v2Tag.Title = $"{title} - Chapter {chapterNumber}: {chapter.tags?.title}";
    id3v2Tag.AlbumArtists = new[] { aaxInfoDto.Format.format?.tags?.artist };
    id3v2Tag.Album = title;
    id3v2Tag.Track = (uint)chapterNumber;
    id3v2Tag.TrackCount = (uint)totalChapters;

    // Set custom frames for chapter information
    id3v2Tag.SetTextFrame("TCHP", chapterNumber.ToString()); // Custom frame for chapter number
    id3v2Tag.SetTextFrame("TCHN", totalChapters.ToString()); // Custom frame for total chapters

    // Add chapter information in the comment
    var chapterInfo = $"Chapter {chapterNumber}/{totalChapters}: {chapter.tags?.title} " +
                      $"(Start: {TimeSpan.FromSeconds(chapter.start):hh\\:mm\\:ss}, " +
                      $"End: {TimeSpan.FromSeconds(chapter.end):hh\\:mm\\:ss})";
    id3v2Tag.Comment = (id3v2Tag.Comment + "\n" + chapterInfo).Trim();

    // Add cover art
    if (File.Exists(coverPath))
    {
        var coverPicture = new Picture(coverPath)
        {
            Type = PictureType.FrontCover,
            Description = "Cover",
            MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
        };
        id3v2Tag.Pictures = new IPicture[] { coverPicture };
    }

    // Add a custom frame for chapter metadata
    var chapterFrame = new UserTextInformationFrame("CHAP")
    {
        Description = "Chapter Information",
        Text = new[] { $"{chapterNumber}/{totalChapters}:{chapter.start}:{chapter.end}:{chapter.tags?.title}" }
    };
    id3v2Tag.AddFrame(chapterFrame);

    // Save the changes
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

        var audioFormat = JsonSerializer.Deserialize<AudioFormatDto>(formatJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var audioChapters = JsonSerializer.Deserialize<AudioChaptersDto>(chaptersJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var audioStreams = JsonSerializer.Deserialize<AudioStreamDto>(streamsJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        //var result = new AaxInfoDto(audioFormat!, audioChapters!, audioStreams!);

        logger.WriteLine(" Done");

        return null;
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