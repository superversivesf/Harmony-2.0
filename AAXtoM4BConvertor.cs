using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Harmony;

public class AaxToM4BConverter
{
    private string ffmpegPath { get; }
    private string ffprobePath { get; }
    private bool quietMode { get; }
    private string inputFolder { get; }
    private string outputFolder { get; }
    private string storageFolder { get; }
    private string workingFolder { get; }
    private Logger logger;

    public AaxToM4BConverter(string inputFolder, string outputFolder, string storageFolder, string workingFolder, bool quietMode = false)
    {
        this.quietMode = quietMode;
        this.inputFolder = inputFolder;
        this.outputFolder = outputFolder;
        this.storageFolder = storageFolder;
        this.workingFolder = workingFolder;
        this.logger = new Logger(quietMode);

        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        this.ffmpegPath = Path.Combine(assemblyDirectory, "ffmpeg");
        this.ffprobePath = Path.Combine(assemblyDirectory, "ffprobe");

        if (!File.Exists(this.ffmpegPath) && !File.Exists(this.ffmpegPath + ".exe"))
        {
            throw new FileNotFoundException($"FFmpeg executable not found in {assemblyDirectory}");
        }
        if (!File.Exists(this.ffprobePath) && !File.Exists(this.ffprobePath + ".exe"))
        {
            throw new FileNotFoundException($"FFprobe executable not found in {assemblyDirectory}");
        }
    }

    public void Execute()
    {
        logger.WriteLine("\bDone");

        logger.Write("Checking folders and purging working files ... ");
        CheckFolders();
        logger.WriteLine("Done");
        var filePaths = Directory.GetFiles(inputFolder, "*.aax").ToList();
        logger.WriteLine($"Found {filePaths.Count} aax files to process\n");
        ProcessAaxFiles(filePaths);
    }

    private void ProcessAaxFiles(List<string> filePaths)
    {
        foreach (var f in filePaths) ProcessAaxFile(f);
    }

    private void ProcessAaxFile(string inputFile)
    {
        var aaxInfo = GetAaxInfo(inputFile);
        PrintAaxInfo(aaxInfo);

        string decryptionKey = ""; // You need to provide the decryption key
        string tempAudioFile = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(inputFile) + ".mp3");
        string chaptersJsonFile = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(inputFile) + "_chapters.json");
        string metadataFile = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(inputFile) + "_metadata.txt");
        string coverFile = Path.Combine(workingFolder, Path.GetFileNameWithoutExtension(inputFile) + "_cover.jpg");

        try
        {
            ExtractAudio(inputFile, decryptionKey, tempAudioFile);
            ExtractChapters(inputFile, chaptersJsonFile);
            CreateMetadataFile(chaptersJsonFile, metadataFile);
            GenerateCover(inputFile, coverFile);

            string outputDirectory = CreateOutputDirectory(aaxInfo);
            string outputFile = Path.Combine(outputDirectory, $"{CleanTitle(aaxInfo.Format.format?.tags?.title)}.m4b");

            CombineAudioChaptersCover(tempAudioFile, coverFile, metadataFile, outputFile);

            MoveCoverFile(coverFile, outputDirectory);
            MoveAaxToStorage(inputFile);
        }
        finally
        {
            CleanupIntermediateFiles(tempAudioFile, chaptersJsonFile, metadataFile);
        }
    }

    private AaxInfoDto GetAaxInfo(string inputFile)
    {
        logger.Write("Probing AAX file ");

        var formatJson = RunProcess(ffprobePath, $"-print_format json -loglevel quiet -show_format \"{inputFile}\"");
        logger.Write(".");

        var chaptersJson = RunProcess(ffprobePath, $"-print_format json -loglevel quiet -show_chapters \"{inputFile}\"");
        logger.Write(".");

        var streamsJson = RunProcess(ffprobePath, $"-print_format json -loglevel quiet -show_streams \"{inputFile}\"");
        logger.WriteLine(" Done");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var audioFormat = JsonSerializer.Deserialize<AudioFormatDto>(formatJson, options);
        var audioChapters = JsonSerializer.Deserialize<AudioChaptersDto>(chaptersJson, options);
        var audioStreams = JsonSerializer.Deserialize<AudioStreamsDto>(streamsJson, options);

        return new AaxInfoDto(audioFormat!, audioChapters!, audioStreams!);
    }

    private void PrintAaxInfo(AaxInfoDto aaxInfo)
    {
        logger.WriteLine($"Title: {CleanTitle(aaxInfo.Format.format?.tags?.title)}");
        logger.WriteLine($"Author(s): {aaxInfo.Format.format?.tags?.artist}");

        if (aaxInfo.Format.format?.duration != null)
        {
            var duration = double.Parse(aaxInfo.Format.format.duration);
            var timeSpan = TimeSpan.FromSeconds(duration);
            logger.WriteLine($"Length: {timeSpan:hh\\:mm\\:ss}");
        }
        else
        {
            logger.WriteLine("Length: 0! Something is wrong");
        }

        logger.WriteLine($"Chapters: {aaxInfo.Chapters.chapters?.Length}");
    }

    private void ExtractAudio(string inputFile, string decryptionKey, string outputFile)
    {
        logger.Write("Extracting audio ... ");
        RunProcessWithSpinner(ffmpegPath, $"-activation_bytes {decryptionKey} -i \"{inputFile}\" -vn -c:a copy \"{outputFile}\"");
        logger.WriteLine("Done");
    }

    private void ExtractChapters(string inputFile, string outputFile)
    {
        logger.Write("Extracting chapters ... ");
        RunProcess(ffprobePath, $"-i \"{inputFile}\" -print_format json -show_chapters -v quiet > \"{outputFile}\"");
        logger.WriteLine("Done");
    }

    private void CreateMetadataFile(string chaptersJsonFile, string outputFile)
    {
        logger.Write("Creating metadata file ... ");
        var chaptersJson = File.ReadAllText(chaptersJsonFile);
        var chapters = JsonSerializer.Deserialize<ChapterInfo>(chaptersJson).chapters;

        var sb = new StringBuilder();
        for (int i = 0; i < chapters.Length; i++)
        {
            var chapter = chapters[i];
            var startTime = (int)(chapter.startTime * 1000);
            var endTime = (int)(chapter.endTime * 1000);
            var title = chapter.tags?.title ?? $"Chapter {i + 1}";

            sb.AppendLine("[CHAPTER]");
            sb.AppendLine("TIMEBASE=1/1000");
            sb.AppendLine($"START={startTime}");
            sb.AppendLine($"END={endTime}");
            sb.AppendLine($"title={title}");
            sb.AppendLine();
        }

        File.WriteAllText(outputFile, sb.ToString());
        logger.WriteLine("Done");
    }

    private void GenerateCover(string inputFile, string coverFile)
    {
        logger.Write("Writing Cover File ... ");
        RunProcessWithSpinner(ffmpegPath, $"-i \"{inputFile}\" -an -codec:v copy \"{coverFile}\"");
        logger.WriteLine("Done");
    }

    private void CombineAudioChaptersCover(string audioFile, string coverFile, string metadataFile, string outputFile)
    {
        logger.Write("Combining audio, chapters, and cover ... ");
        RunProcessWithSpinner(ffmpegPath, $"-i \"{audioFile}\" -i \"{coverFile}\" -map 0:a -map 1 -c copy -c:v:1 png " +
                               $"-disposition:v:0 attached_pic -metadata:s:v:0 mimetype=image/jpeg " +
                               $"-metadata:s:v:0 handler_name=\"Cover Art\" -metadata:s:v:0 handler=\"Cover Art\" " +
                               $"-f ffmetadata -i \"{metadataFile}\" -map_metadata 2 -movflags +faststart \"{outputFile}\"");
        logger.WriteLine("Done");
    }

    private string CreateOutputDirectory(AaxInfoDto aaxInfo)
    {
        var title = CleanTitle(aaxInfo.Format.format?.tags?.title);
        var author = CleanAuthor(aaxInfo.Format.format?.tags?.artist);
        var outputDirectory = Path.Combine(outputFolder, author, title ?? string.Empty);
        var invalidPathChars = Path.GetInvalidPathChars();
        foreach (var c in invalidPathChars) outputDirectory = outputDirectory.Replace(c, '_');

        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    private void MoveCoverFile(string coverFile, string outputDirectory)
    {
        logger.Write("Moving Cover file ... ");
        var coverFileDestination = Path.Combine(outputDirectory, "Cover.jpg");
        File.Move(coverFile, coverFileDestination, true);
        logger.WriteLine("Done");
    }

    private void MoveAaxToStorage(string inputFile)
    {
        logger.Write("Moving AAX file to storage ... ");
        var storageFile = Path.Combine(storageFolder, Path.GetFileName(inputFile));
        File.Move(inputFile, storageFile, true);
        logger.WriteLine("Done");
    }

    private void CleanupIntermediateFiles(params string[] files)
    {
        logger.Write("Cleaning up intermediate files ... ");
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        logger.WriteLine("Done\n");
    }

    private void CheckFolders()
    {
        if (!Directory.Exists(inputFolder)) throw new Exception("Input folder does not exist: " + inputFolder);
        if (!Directory.Exists(outputFolder)) throw new Exception("Output folder does not exist: " + outputFolder);
        if (!Directory.Exists(storageFolder)) throw new Exception("Storage folder does not exist: " + storageFolder);
        if (!Directory.Exists(workingFolder)) throw new Exception("Working folder does not exist: " + workingFolder);

        var di = new DirectoryInfo(workingFolder);
        foreach (var file in di.GetFiles()) file.Delete();
    }

    private string RunProcess(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = File.Exists(fileName) ? fileName : fileName + ".exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Process exited with code {process.ExitCode}: {process.StandardError.ReadToEnd()}");
        }

        return output;
    }

    private void RunProcessWithSpinner(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = File.Exists(fileName) ? fileName : fileName + ".exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();

        while (!process.HasExited)
        {
            logger.AdvanceSpinner();
            Thread.Sleep(100);
        }

        if (process.ExitCode != 0)
        {
            throw new Exception($"Process exited with code {process.ExitCode}: {process.StandardError.ReadToEnd()}");
        }
    }

    private string CleanTitle(string? title)
    {
        return title?.Replace("(Unabridged)", string.Empty).Replace(":", " -").Replace("'", string.Empty)
            .Replace("?", string.Empty).Trim();
    }

    private string CleanAuthor(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";

        var authors = name.Split(',');
        if (authors.Length > 4) return "Various";

        return name.Replace("Jr.", "Jr").Trim();
    }
}

public class AaxInfoDto
{
    public AudioFormatDto Format { get; set; }
    public AudioChaptersDto Chapters { get; set; }
    public AudioStreamsDto Streams { get; set; }

    public AaxInfoDto(AudioFormatDto format, AudioChaptersDto chapters, AudioStreamsDto streams)
    {
        Format = format;
        Chapters = chapters;
        Streams = streams;
    }
}

public class AudioFormatDto
{
    public FormatInfo format { get; set; }
}

public class FormatInfo
{
    public string? duration { get; set; }
    public TagInfo? tags { get; set; }
}

public class TagInfo
{
    public string? title { get; set; }
    public string? artist { get; set; }
}

public class AudioChaptersDto
{
    public Chapter[]? chapters { get; set; }
}

public class AudioStreamsDto
{
    public Stream[]? streams { get; set; }
}

public class ChapterInfo
{
    public Chapter[] chapters { get; set; }
}

public class Chapter
{
    public double startTime { get; set; }
    public double endTime { get; set; }
    public Tags tags { get; set; }
}

public class Tags
{
    public string title { get; set; }
}

public class Stream
{
    // Add properties as needed
}

