using System.Diagnostics;
using System.Text.Json;
using TagLib;
using Xabe.FFmpeg;
using File = System.IO.File;

namespace Harmony
{
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
            var filePaths = Directory.GetFiles(_inputFolder, "*.aax");
            logger.WriteLine($"Found {filePaths.Length} aax files to process\n");
            foreach (var filePath in filePaths)
            {
                ProcessAaxFile(filePath);
            }
        }

        private void ProcessAaxFile(string filePath)
        {
            var logger = new Logger(_quietMode);

            logger.WriteLine("Processing " + Path.GetFileName(filePath) + " ...");
            
            var aaxInfo = GetAaxInfo(filePath);
            WriteAaxInfo(aaxInfo, logger);

            var outputDirectory = PrepareOutputDirectory(aaxInfo);
            var intermediateFile = ProcessToM4A(aaxInfo, filePath, outputDirectory);
            var coverFile = GenerateCover(filePath, outputDirectory);

            AddMetadataToM4A(intermediateFile, aaxInfo);
            AddCoverArtToM4A(intermediateFile, coverFile);

            var m4BFilePath = Path.ChangeExtension(intermediateFile, "m4b");
            File.Move(intermediateFile, m4BFilePath);

            logger.WriteLine($"Successfully converted {Path.GetFileName(filePath)} to M4B.");

            // Move original AAX to storage
            var storageFile = Path.Combine(_storageFolder, Path.GetFileName(filePath));
            File.Move(filePath, storageFile);

            // Cleanup
            File.Delete(coverFile);
        }

        private AaxInfoDto? GetAaxInfo(string filePath)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Probing AAX file... ");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v quiet -print_format json -show_format -show_chapters -activation_bytes {_activationBytes} \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            logger.WriteLine("Done");

            return JsonSerializer.Deserialize<AaxInfoDto>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private void WriteAaxInfo(AaxInfoDto? aaxInfo, Logger logger)
        {
            logger.WriteLine($"Title: {CleanTitle(aaxInfo?.format?.tags?.title)}");
            logger.WriteLine($"Author(s): {aaxInfo?.format?.tags?.artist}");
            if (aaxInfo?.format?.duration != null)
                logger.WriteLine(
                    $"Length: {TimeSpan.FromSeconds(double.Parse(aaxInfo.format.duration)):hh\\:mm\\:ss}");
            if (aaxInfo?.chapters != null) logger.WriteLine($"Chapters: {aaxInfo.chapters.Count}");
        }

        private string PrepareOutputDirectory(AaxInfoDto? aaxInfo)
        {
            var title = CleanTitle(aaxInfo?.format?.tags?.title);
            var author = CleanAuthor(aaxInfo?.format?.tags?.artist); 
            var outputDirectory = Path.Combine(_outputFolder, author,title);
            Directory.CreateDirectory(outputDirectory);
            return outputDirectory;
            
        }

        private string ProcessToM4A(AaxInfoDto aaxInfo, string filePath, string outputDirectory)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Converting AAX to M4A... ");
            
            /*
             2024-09-22 20:28:58+0000 DEBUG Book and Variable values
               =Start==========================================================================
               title                 = A Brief History of the Philosophy of Time
               auth_code             = 8e4ab703
               aaxc                  = 0
               aaxc_key              = 
               aaxc_iv               = 
               mode                  = chaptered
               aax_file              = ABriefHistoryofthePhilosophyofTime_ep6.aax
               container             = mp4
               codec                 = copy
               bitrate               = 64k
               artist                = Adrian Bardon
               album_artist          = Adrian Bardon
               album                 = A Brief History of the Philosophy of Time
               album_date            = 2014
               genre                 = Audiobook
               copyright             = ©2013 Oxford University Press (P)2014 Audible Inc.
               narrator              = 
               description           = 
               publisher             = 
               currentDirNameScheme  = Audiobook/Adrian Bardon/A Brief History of the Philosophy of Time
               output_directory      = ./Audiobook/Adrian Bardon/A Brief History of the Philosophy of Time
               currentFileNameScheme = A Brief History of the Philosophy of Time
               output_file           = ./Audiobook/Adrian Bardon/A Brief History of the Philosophy of Time/A Brief History of the Philosophy of Time.m4b
               metadata_file         = /tmp/tmp.JNqZMbPPo4/metadata.txt
               working_directory     = /tmp/tmp.JNqZMbPPo4
               =End============================================================================
               2024-09-22 20:28:58+0000 Total length: 05:23:54
               2024-09-22 20:28:58+0000 DEBUG "$FFMPEG" -loglevel error -stats ${decrypt_param} -i "${aax_file}" -vn -codec:a "${codec}" -ab ${bitrate} -map_metadata -1 -metadata title="${title}" -metadata artist="${artist}" -metadata album_artist="${album_artist}" -metadata album="${album}" -metadata date="${album_date}" -metadata track="1/1" -metadata genre="${genre}" -metadata copyright="${copyright}" "${output_file}"
               size=  150608kB time=05:23:54.40 bitrate=  63.5kbits/s speed=1.43e+04x    
               2024-09-22 20:29:00+0000 Created ./Audiobook/Adrian Bardon/A Brief History of the Philosophy of Time/A Brief History of the Philosophy of Time.m4b.
               2024-09-22
              
             */
            //             var chapterFile = $"{title}-{chapterNumber.ToString(formatString)}.mp3";
            
            var title = CleanTitle(aaxInfo.format?.tags?.title);
            var filename = title; 
            var outputFile = Path.Combine(outputDirectory, filename + ".m4a");
            var conversion = FFmpeg.Conversions.New()
                //.AddParameter(" -loglevel error -stats")
                .AddParameter($"-activation_bytes \"{_activationBytes}\" ")
                .AddParameter($"-i \"{filePath}\" ")
                .AddParameter("-vn -codec:a copy -b:a 64k ")
                .SetOutput(outputFile);
            
            conversion.Start().Wait();

            logger.WriteLine("Done");
            return outputFile;
        }

        private string GenerateCover(string filePath, string outputDirectory)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Extracting cover art... ");

            var coverFile = Path.Combine(outputDirectory, "cover.jpg");
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-activation_bytes {_activationBytes}")
                .AddParameter($"-i \"{filePath}\"")
                .AddParameter("-an -vcodec copy")
                .SetOutput(coverFile);

            conversion.Start().Wait();

            logger.WriteLine("Done");
            return coverFile;
        }

        private void AddMetadataToM4A(string filePath, AaxInfoDto? aaxInfo)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Adding metadata to M4A... ");

            using (var file = TagLib.File.Create(filePath))
            {
                var tag = file.GetTag(TagTypes.Apple);
                tag.Title = aaxInfo?.format?.tags?.title;
                tag.Performers = new[] { aaxInfo?.format?.tags?.artist};
                tag.Album = aaxInfo?.format?.tags?.album;
                if (aaxInfo?.format?.tags?.date != null)
                    tag.Year = uint.Parse(aaxInfo.format.tags.date);
                tag.Genres = new[] { aaxInfo?.format?.tags?.genre };
                tag.Copyright = aaxInfo?.format?.tags?.copyright;
                file.Save();
            }

            logger.WriteLine("Done");
        }

        private void AddCoverArtToM4A(string filePath, string coverPath)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Adding cover art to M4A... ");

            using (var file = TagLib.File.Create(filePath))
            {
                var picture = new Picture(coverPath);
                file.Tag.Pictures = new IPicture[] { picture };
                file.Save();
            }

            logger.WriteLine("Done");
        }

        private string? CleanTitle(string? title)
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

        private void CheckFolders()
        {
            if (!Directory.Exists(_inputFolder)) throw new Exception("Input folder does not exist: " + _inputFolder);
            if (!Directory.Exists(_outputFolder)) throw new Exception("Output folder does not exist: " + _outputFolder);
            if (!Directory.Exists(_storageFolder)) throw new Exception("Storage folder does not exist: " + _storageFolder);
            if (!Directory.Exists(_workingFolder)) throw new Exception("Working folder does not exist: " + _workingFolder);

            foreach (var file in Directory.GetFiles(_workingFolder))
            {
                File.Delete(file);
            }
        }
    }
}