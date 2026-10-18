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

    /// <summary>
    /// The decision of which files should run through each converter. A book that exists
    /// as both AAX and AAXC converts only whichever source file is newer; an unconvertible
    /// AAX falls back to its paired AAXC when one exists.
    /// </summary>
    internal record ConversionPlan(
        IReadOnlyList<string> AaxFilesToConvert,
        IReadOnlyList<string> AaxcFilesToConvert,
        IReadOnlyList<string> AaxFilesSkippedByNewerAaxc,
        IReadOnlyList<string> AaxcFilesSkippedByNewerAax,
        bool WarnMissingActivationBytes);

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

    /// <summary>
    /// Decides which files should be converted. Files pair by filename stem: for a stem
    /// that exists in both formats, only the newer file converts (a tie favors AAXC, which
    /// never needs activation bytes). An AAX file cannot convert without activation bytes;
    /// if its stem has an AAXC fallback the book still converts via that fallback, otherwise
    /// WarnMissingActivationBytes signals the caller should tell the user.
    /// </summary>
    internal static ConversionPlan BuildConversionPlan(string[] aaxFiles, string[] aaxcFiles, string? activationBytes)
    {
        var hasActivationBytes = !string.IsNullOrWhiteSpace(activationBytes);
        var newestAaxByStem = GroupNewestFileByStem(aaxFiles);
        var newestAaxcByStem = GroupNewestFileByStem(aaxcFiles);

        var aaxFilesToConvert = new List<string>();
        var aaxcFilesToConvert = new List<string>();
        var aaxFilesSkippedByNewerAaxc = new List<string>();
        var aaxcFilesSkippedByNewerAax = new List<string>();
        var warnMissingActivationBytes = false;

        foreach (var (stem, aaxFile) in newestAaxByStem)
        {
            if (!newestAaxcByStem.TryGetValue(stem, out var aaxcFile))
            {
                if (hasActivationBytes)
                {
                    aaxFilesToConvert.Add(aaxFile);
                }
                else
                {
                    warnMissingActivationBytes = true;
                }

                continue;
            }

            var aaxIsNewer = File.GetLastWriteTimeUtc(aaxFile) > File.GetLastWriteTimeUtc(aaxcFile);
            if (aaxIsNewer && hasActivationBytes)
            {
                aaxFilesToConvert.Add(aaxFile);
                aaxcFilesSkippedByNewerAax.Add(aaxcFile);
            }
            else
            {
                aaxcFilesToConvert.Add(aaxcFile);
                if (!aaxIsNewer)
                    aaxFilesSkippedByNewerAaxc.Add(aaxFile);
            }
        }

        foreach (var aaxcFile in newestAaxcByStem.Values)
        {
            if (!newestAaxByStem.ContainsKey(GetFileNameStem(aaxcFile)))
                aaxcFilesToConvert.Add(aaxcFile);
        }

        return new ConversionPlan(
            aaxFilesToConvert,
            aaxcFilesToConvert,
            aaxFilesSkippedByNewerAaxc,
            aaxcFilesSkippedByNewerAax,
            warnMissingActivationBytes);
    }

    /// <summary>
    /// Reduces files that share a filename stem (case-insensitive) down to the single
    /// most recently written one, so pairing compares each book's newest copy per format.
    /// </summary>
    private static Dictionary<string, string> GroupNewestFileByStem(IEnumerable<string> filePaths)
    {
        return filePaths
            .GroupBy(GetFileNameStem, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(File.GetLastWriteTimeUtc).First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string GetFileNameStem(string filePath) => Path.GetFileNameWithoutExtension(filePath);

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
        var activationBytes = options.ActivationBytes;
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

        // Discover source files and decide which ones should be converted
        var aaxFiles = Directory.GetFiles(inputFolder!, "*.aax", SearchOption.AllDirectories);
        var aaxcFiles = Directory.GetFiles(inputFolder!, "*.aaxc", SearchOption.AllDirectories);
        var conversionPlan = BuildConversionPlan(aaxFiles, aaxcFiles, activationBytes);

        if (conversionPlan.WarnMissingActivationBytes)
        {
            logger.WriteLine(
                "WARNING: AAX file(s) found but no activation bytes were provided.");
            logger.WriteLine("AAX files require the -a/--ActivationBytes option and will be skipped.");
        }

        foreach (var skippedFile in conversionPlan.AaxFilesSkippedByNewerAaxc)
            logger.WriteLine($"Skipping {Path.GetFileName(skippedFile)}: a newer AAXC version of this book exists.");

        foreach (var skippedFile in conversionPlan.AaxcFilesSkippedByNewerAax)
            logger.WriteLine($"Skipping {Path.GetFileName(skippedFile)}: a newer AAX version of this book exists.");

        var skipAaxFiles = aaxFiles
            .Except(conversionPlan.AaxFilesToConvert)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skipAaxcFiles = aaxcFiles
            .Except(conversionPlan.AaxcFilesToConvert)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var totalFiles = conversionPlan.AaxFilesToConvert.Count + conversionPlan.AaxcFilesToConvert.Count;

        if (totalFiles == 0)
        {
            logger.WriteLine("No AAX or AAXC files found to process.");
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
                if (conversionPlan.AaxFilesToConvert.Count > 0)
                {
                    // Create AAX converter with progress manager and execute
                    var aaxConverter = new AaxToM4BConvertor(
                        activationBytes!,
                        bitrate,
                        quietMode,
                        inputFolder!,
                        outputFolder!,
                        clobber,
                        library,
                        ctx,
                        skipAaxFiles
                    );
                    await aaxConverter.ExecuteAsync().ConfigureAwait(false);
                }

                if (conversionPlan.AaxcFilesToConvert.Count > 0)
                {
                    // Create AAXC converter with progress manager and execute
                    var aaxcConvertor = new AaxcToM4BConvertor(
                        bitrate,
                        quietMode,
                        inputFolder!,
                        outputFolder!,
                        clobber,
                        library,
                        ctx,
                        skipAaxcFiles
                    );
                    await aaxcConvertor.ExecuteAsync().ConfigureAwait(false);
                }
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

        [Option('a', "ActivationBytes", Required = false,
            HelpText =
                "Activation bytes for decoding your AAX File. See https://github.com/inAudible-NG/tables for details of how to obtain")]
        public string? ActivationBytes { get; set; }

        [Option('f', "FetchFFMpeg", Required = false,
            HelpText = "Just download versions of ffmpeg and ffprobe locally and exit")]
        public bool FetchFFMpeg { get; set; }

        [Option('q', "QuietMode", Required = false, HelpText = "Disable all output", Default = false)]
        public bool QuietMode { get; set; }
    }
}