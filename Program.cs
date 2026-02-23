using System.Globalization;
using System.Reflection;
using CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using Harmony.Dto;
using Xabe.FFmpeg.Downloader;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Harmony;

internal static class Program
{
    /// <summary>
    /// Delay in milliseconds between spinner animation updates.
    /// </summary>
    public const int SpinnerDelayMs = 50;

    private static void Main(string[] args)
    {
        // dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r linux-arm -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r linux-arm64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        try
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Processing failure -> {e.Message}:{e.InnerException?.Message}");
        }
    }

    private static void HandleParseError(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine($"CLI Error: {error.Tag}");
        }
        Environment.Exit(1);
    }

    /// <summary>
    /// Fetches the latest FFmpeg version asynchronously with a spinner animation.
    /// </summary>
    /// <param name="logger">Logger instance for output.</param>
    public static async Task FetchFFmpegAsync(Logger logger)
    {
        logger.Write("Fetching Latest FFMpeg ...  ");

        var fetchTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        while (!fetchTask.IsCompleted)
        {
            logger.AdvanceSpinner();
            await Task.Delay(SpinnerDelayMs);
        }

        logger.WriteLine("\bDone");
    }

    private static void RunOptions(Options options)
    {
        var clobber = options.Clobber;
        var activationBytes = options.ActivationBytes;
        var bitrate = options.Bitrate;
        var inputFolder = options.InputFolder;
        var outputFolder = options.OutputFolder;
        var quietMode = options.QuietMode;
        var logger = new Logger(quietMode);
        var loopMode = options.LoopMode;

        logger.WriteLine(
            $"Harmony {Assembly.GetExecutingAssembly().GetName().Version}\nCopyright(C) 2023 Harmony\n");

        if (options.FetchFFMpeg)
        {
            FetchFFmpegAsync(logger).GetAwaiter().GetResult();
            return;
        }

        do
        {
            FetchFFmpegAsync(logger).GetAwaiter().GetResult();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = "\t"
            };
            var libraryFile = Path.Combine(inputFolder!, "library.tsv");
            List<AudibleLibraryDto>? library = null;
            if (File.Exists(libraryFile))
            {
                using (var reader = new StreamReader(libraryFile))
                using (var csv = new CsvReader(reader, config))
                {
                    library = csv.GetRecords<AudibleLibraryDto>().ToList();
                }
            }

            var aaxConverter = new AaxToM4BConvertor(
                activationBytes!,
                bitrate,
                quietMode,
                inputFolder!,
                outputFolder!,
                clobber,
                library
            );
            aaxConverter.ExecuteAsync().GetAwaiter().GetResult();

            var aaxcConvertor = new AaxcToM4BConvertor(
                bitrate,
                quietMode,
                inputFolder!,
                outputFolder!,
                clobber,
                library
            );
            aaxcConvertor.ExecuteAsync().GetAwaiter().GetResult();

            if (loopMode)
            {
                logger.WriteLine("Run complete, sleeping for 5 minutes");
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }

            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        } while (loopMode);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public class Options
    {
        [Option('c', "Clobber", Required = false,
            HelpText = "Delete or skip existing output files?", Default = false)]
        public bool Clobber { get; set; }

        [Option('b', "Bitrate", Required = false,
            HelpText = "The bitrate in kilobits for the output files. Defaults to 64k, specified as 64", Default = 64)]
        public int Bitrate { get; set; }

        [Option('i', "InputFolder", Required = false, HelpText = "The folder that contains your AAX files")]
        public string? InputFolder { get; set; }

        [Option('o', "OutputFolder", Required = false, HelpText = "The folder that will contain your M4B files")]
        public string? OutputFolder { get; set; }

        [Option('a', "ActivationBytes", Required = false,
            HelpText =
                "Activation bytes for decoding your AAX File. See https://github.com/inAudible-NG/tables for details of how to obtain")]
        public string? ActivationBytes { get; set; }

        [Option('f', "FetchFFMpeg", Required = false,
            HelpText = "Just download versions of ffmpeg and ffprobe locally and exit")]
        public bool FetchFFMpeg { get; set; }

        [Option('q', "QuietMode", Required = false, HelpText = "Disable all output", Default = false)]
        public bool QuietMode { get; set; }

        [Option('l', "LoopMode", Required = false,
            HelpText =
                "Have the program run to completion then check and run again with a 5 minute sleep between each run",
            Default = false)]
        public bool LoopMode { get; set; }
    }
}