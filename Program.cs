using System.Reflection;
using CommandLine;
using Xabe.FFmpeg.Downloader;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Harmony;

internal static class Program
{
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
            Console.WriteLine("Processing failure -> " + e.Message + ":" + e.InnerException?.Message);
        }
    }

    private static void HandleParseError(IEnumerable<Error> errors)
    {
    }

    private static void RunOptions(Options options)
    {
        var activationBytes = options.activationBytes;
        var bitrate = options.bitrate;
        var inputFolder = options.inputFolder;
        var outputFolder = options.outputFolder;
        var storageFolder = options.storageFolder;
        var quietMode = options.quietMode;
        var workingFolder = options.workingFolder;
        var logger = new Logger(quietMode);
        var loopMode = options.loopMode;

        logger.WriteLine(
            $"Harmony {Assembly.GetExecutingAssembly().GetName().Version!.ToString()}\nCopyright(C) 2023 Harmony\n");

        if (options.fetchFFMpeg)
        {
            logger.Write("Fetching Latest FFMpeg ...  ");

            var fetchTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            while (!fetchTask.IsCompleted)
            {
                logger.AdvanceSpinner();
                Thread.Sleep(50);
            }

            logger.WriteLine("\bDone");

            return;
        }

        do
        {
            var aaxProcessor = new AaxAudioConvertor(activationBytes!, bitrate, quietMode, inputFolder!, outputFolder!,
                storageFolder!, workingFolder!);

            aaxProcessor.Execute();

            if (loopMode)
            {
                logger.WriteLine("Run complete, sleeping for 5 minutes");
                Thread.Sleep(600);
            }

            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        } while (loopMode);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public class Options
    {
        [Option('b', "Bitrate", Required = false,
            HelpText = "The bitrate in kilobits for the output files. Defaults to 64k, specified as 64", Default = 64)]
        public int bitrate { get; set; }

        [Option('i', "InputFolder", Required = false, HelpText = "The folder that contains your AAX files")]
        public string? inputFolder { get; set; } = null;

        [Option('o', "OutputFolder", Required = false, HelpText = "The folder that will contain your MP3 files")]
        public string? outputFolder { get; set; } = null;

        [Option('s', "StorageFolder", Required = false, HelpText = "Folder that finished AAX files will be moved too")]
        public string? storageFolder { get; set; } = null;

        [Option('w', "WorkingFolder", Required = false,
            HelpText = "Temp folder that will be used for processing everything. Emptied at start up")]
        public string? workingFolder { get; set; } = null;

        [Option('a', "ActivationBytes", Required = false,
            HelpText =
                "Activation bytes for decoding your AAX File. See https://github.com/inAudible-NG/tables for details of how to obtain")]
        public string? activationBytes { get; set; } = null;

        [Option('f', "FetchFFMpeg", Required = false,
            HelpText = "Just download versions of ffmpeg and ffprobe locally and exit")]
        // ReSharper disable once InconsistentNaming
        public bool fetchFFMpeg { get; set; }

        [Option('q', "QuietMode", Required = false, HelpText = "Disable all output", Default = false)]
        public bool quietMode { get; set; }

        [Option('l', "LoopMode", Required = false,
            HelpText =
                "Have the program run to completion then check and run again with a 5 minute sleep between each run",
            Default = false)]
        public bool loopMode { get; set; }
    }
}