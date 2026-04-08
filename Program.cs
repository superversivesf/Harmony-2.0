using System.Globalization;
using System.Reflection;
using System.Threading;
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

    private static async Task Main(string[] args)
    {
        // dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r linux-arm -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r linux-arm64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        // dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
        try
        {
            var result = await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options => await RunOptionsAsync(options).ConfigureAwait(false));
            
            if (result.Tag == ParserResultType.NotParsed)
            {
                HandleParseError(((NotParsed<Options>)result).Errors);
            }
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

    private static async Task RunOptionsAsync(Options options)
    {
        var clobber = options.Clobber;
        var bitrate = options.Bitrate;
        var inputFolder = options.InputFolder;
        var outputFolder = options.OutputFolder;
        var quietMode = options.QuietMode;
        var logger = new Logger(quietMode);

        logger.WriteLine(
            $"Harmony {Assembly.GetExecutingAssembly().GetName().Version}\nCopyright(C) 2023 Harmony\n");

        if (options.FetchFFMpeg)
        {
            await FetchFFmpegAsync(logger).ConfigureAwait(false);
            return;
        }

        await FetchFFmpegAsync(logger).ConfigureAwait(false);

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

        // Check for AAX files and warn user (AAX is deprecated)
        var aaxFiles = Directory.GetFiles(inputFolder!, "*.aax", SearchOption.AllDirectories);
        if (aaxFiles.Length > 0)
        {
            logger.WriteLine("WARNING: AAX files are no longer supported.");
            logger.WriteLine($"Found {aaxFiles.Length} AAX file(s). Please use AAXC files instead.");
            logger.WriteLine("AAX files require activation bytes which are no longer supported in this version.");
            return;
        }

        // Count total AAXC files before starting progress display
        var aaxcFiles = Directory.GetFiles(inputFolder!, "*.aaxc", SearchOption.AllDirectories);
        var totalFiles = aaxcFiles.Length;

        if (totalFiles == 0)
        {
            logger.WriteLine("No AAXC files found to process.");
            return;
        }

        // Setup CancellationTokenSource for graceful shutdown on Ctrl+C
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            logger.WriteLine("\nCancellation requested. Finishing current file...");
        };

        // Create ProgressContextManager with total file count
        using var progressManager = new ProgressContextManager(totalFiles, cts.Token);

        try
        {
            // Wrap conversion execution in ProgressContextManager.RunAsync
            await progressManager.RunAsync(async ctx =>
            {
                // Create AAXC converter with progress manager and execute
                var aaxcConvertor = new AaxcToM4BConvertor(
                    bitrate,
                    quietMode,
                    inputFolder!,
                    outputFolder!,
                    clobber,
                    library,
                    ctx
                );
                await aaxcConvertor.ExecuteAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.WriteLine("Operation cancelled by user.");
        }
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

        [Option('f', "FetchFFMpeg", Required = false,
            HelpText = "Just download versions of ffmpeg and ffprobe locally and exit")]
        public bool FetchFFMpeg { get; set; }

        [Option('q', "QuietMode", Required = false, HelpText = "Disable all output", Default = false)]
        public bool QuietMode { get; set; }
    }
}