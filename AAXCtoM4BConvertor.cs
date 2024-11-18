using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Harmony.Dto;
using Newtonsoft.Json;
using TagLib;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;
using File = System.IO.File;
using JsonSerializer = System.Text.Json.JsonSerializer;
using SearchOption = System.IO.SearchOption;

namespace Harmony
{
    internal class AaxcToM4BConvertor
    {
        private readonly int _bitrate;
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly bool _quietMode;
        private List<AudibleLibraryDto>? _library;
        private readonly bool _clobber;
        private string? _iv;
        private string? _key; 

        public AaxcToM4BConvertor(int bitrate, bool quietMode, string inputFolder,
            string outputFolder,bool clobber, List<AudibleLibraryDto>? library = null)
        {
            _clobber = clobber;
            _bitrate = bitrate;
            _quietMode = quietMode;
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;
            _library = library;
        }

        internal void Execute()
        {
            var logger = new Logger(_quietMode);
            logger.WriteLine("\bDone");

            logger.Write("Checking folders and purging working files ... ");
            CheckFolders();
            logger.WriteLine("Done");
            var filePaths = Directory.GetFiles(_inputFolder, "*.aaxc", SearchOption.AllDirectories);
            logger.WriteLine($"Found {filePaths.Length} aaxc files to process\n");
            foreach (var filePath in filePaths)
            {
                ProcessAaxcFile(filePath);
            }
        }

        private void ProcessAaxcFile(string filePath)
        {
            var logger = new Logger(_quietMode);

            logger.WriteLine("Processing " + Path.GetFileName(filePath) + " ...");
            
            
            // Need to get the voucher and find the IV and Key

            var voucherFile = Path.ChangeExtension(filePath, "voucher");
            var voucher =  JsonConvert.DeserializeObject<AudibleVoucherDto>(File.ReadAllText(voucherFile));

            this._iv = voucher?.content_license.license_response.iv;
            this._key = voucher?.content_license.license_response.key;
            
            var aaxInfo = GetAaxcInfo(filePath);
            WriteAaxcInfo(aaxInfo, logger);

            var regex = new Regex("Part_[0-9]-LC");
            var boxset = regex.Match(filePath).Success;
            var outputDirectory = PrepareOutputDirectory(aaxInfo, boxset);
            var intermediateFile = ProcessToM4A(aaxInfo, filePath, outputDirectory);

            if (intermediateFile == null)
                return;
            
            var m4BFilePath = intermediateFile.Replace(".m4a", "-nochapter.m4b");
            var coverFile = GenerateCover(filePath, outputDirectory);
            var chapterFile = Path.Combine(outputDirectory, "chapter.txt");
            ChapterConverter.CreateChapterFile(filePath, aaxInfo, chapterFile);
            
            AddMetadataToM4A(intermediateFile, aaxInfo, coverFile);

            File.Move(intermediateFile, m4BFilePath);

            var analyser = new FFprobeAnalyzer();

            var goodFileCheck = analyser.AnalyzeFile(m4BFilePath);
            goodFileCheck.Wait();

            if (!goodFileCheck.Result)
            {
                logger.WriteLine("Problem with file, trying fallback processing");
                File.Delete(m4BFilePath);
                outputDirectory = PrepareOutputDirectory(aaxInfo, boxset); 
                intermediateFile = FallbackProcessToM4A(aaxInfo, filePath, outputDirectory); 
                coverFile = GenerateCover(filePath, outputDirectory);

                AddMetadataToM4A(intermediateFile, aaxInfo, coverFile);

                m4BFilePath = intermediateFile.Replace(".m4a", "-nochapter.m4b");
                File.Move(intermediateFile, m4BFilePath);
            }

            logger.Write("Adding Chapters ...      ");

            var outputFile = Path.Combine(outputDirectory, m4BFilePath.Replace("-nochapter", String.Empty));
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{m4BFilePath}\" ")
                .AddParameter($"-i \"{chapterFile}\" ")
                .AddParameter($"-map_metadata 1 -codec copy")
                .SetOutput(outputFile);
                
            //conversion.OnProgress += ProgressMeter;
            conversion.Start().Wait();
                
            logger.WriteLine("Done");
            
            File.Delete(chapterFile);
            File.Delete(m4BFilePath);
            
            
            logger.WriteLine($"Successfully converted {Path.GetFileName(filePath)} to M4B.");

            // Move original AAX to storage
            //var storageFile = Path.Combine(_storageFolder, Path.GetFileName(filePath));
            //File.Move(filePath, storageFile);

            // Cleanup
            //File.Delete(coverFile);
        }

        private AaxInfoDto? GetAaxcInfo(string filePath)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Probing AAXC file... ");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v quiet -print_format json -show_format -show_chapters -audible_key {_key} -audible_iv {_iv} \"{filePath}\"",
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

        private void WriteAaxcInfo(AaxInfoDto? aaxInfo, Logger logger)
        {
            logger.WriteLine($"Title: {CleanTitle(aaxInfo?.format?.tags?.title)}");
            logger.WriteLine($"Author(s): {aaxInfo?.format?.tags?.artist}");
            if (aaxInfo?.format?.duration != null)
                logger.WriteLine(
                    $"Length: {TimeSpan.FromSeconds(double.Parse(aaxInfo.format.duration)):hh\\:mm\\:ss}");
            if (aaxInfo?.chapters != null) logger.WriteLine($"Chapters: {aaxInfo.chapters.Count}");
        }

        private string PrepareOutputDirectory(AaxInfoDto? aaxInfo, bool boxset)
        {
            string? title = CleanTitle(aaxInfo?.format?.tags?.title);;
            if (boxset)
            { 
                title = Regex.Replace(title, @"\bPart [1-9]\b", "").Trim();
            }

            var author = CleanAuthor(aaxInfo?.format?.tags?.artist); 
            var outputDirectory = Path.Combine(_outputFolder, author,title);
            Directory.CreateDirectory(outputDirectory);
            return outputDirectory;
            
        }

        private string FallbackProcessToM4A(AaxInfoDto aaxInfo, string filePath, string outputDirectory)
        {
            var logger = new Logger(_quietMode);

            logger.Write("Converting AAX to WAV ...      ");
            
            var title = CleanTitle(aaxInfo.format?.tags?.title);
            var filename = title; 
            var outputFile = Path.Combine(outputDirectory, filename + ".wav");
            var conversion = FFmpeg.Conversions.New()
                //.AddParameter(" -loglevel error -stats")
                .AddParameter($"-audible_key \"{_key}\" ")
                .AddParameter($"-audible_iv \"{_iv}\" ")
                .AddParameter($"-i \"{filePath}\" ")
                .AddParameter("-vn -codec:a pcm_s16le -q:a 0 ")
                .SetOutput(outputFile);

            //conversion.OnProgress += ProgressMeter;
            conversion.Start().Wait();
            Console.WriteLine();
            logger.Write("Converting WAV to M4A...       ");

            filePath = outputFile;
            outputFile = Path.Combine(outputDirectory, filename + ".m4a");
            conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{filePath}\" ")
                .AddParameter("-vn -codec:a aac -b:a 64k ")
                .SetOutput(outputFile);

            //conversion.OnProgress += ProgressMeter;
            conversion.Start().Wait();
            
            File.Delete(filePath);
            //File.Delete(chapterFile);
            Console.WriteLine();
            
            //File.Delete(filePath);
            //File.Delete(chapterFile);
            
            return outputFile;
        }
        
        private string? ProcessToM4A(AaxInfoDto aaxInfo, string filePath, string outputDirectory)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Converting AAX to M4A...      ");
            
            var title = CleanTitle(aaxInfo.format?.tags?.title);
            var filename = title; 
            var outputFile = Path.Combine(outputDirectory, filename + ".m4a");

            var m4BFilePath = Path.ChangeExtension(outputFile, "m4b");

            if (File.Exists(m4BFilePath) && this._clobber)
            {
                logger.WriteLine("\nFile Already Exists ... Deleting");

                File.Delete(m4BFilePath);
            }
            else if (File.Exists(m4BFilePath))
            {
                logger.WriteLine("\nFile Already Exists ... Skipping");
                return null;
            }

            
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            
            var conversion = FFmpeg.Conversions.New()
                //.AddParameter(" -loglevel error -stats")
                .AddParameter($"-audible_key \"{_key}\" ")
                .AddParameter($"-audible_iv \"{_iv}\" ")
                .AddParameter($"-i \"{filePath}\" ")
                .AddParameter("-map_chapters -1 -vn -codec:a copy -b:a 64k ")
                .SetOutput(outputFile);

            //conversion.OnProgress += ProgressMeter;
            conversion.Start().Wait();
            Console.WriteLine();
            
            return outputFile;
        }

        private void ProgressMeter(object sender, ConversionProgressEventArgs args)
        {
            Console.Write("\b\b\b\b\b\b[" + args.Percent.ToString("D3") + "%]");
        }

        private string GenerateCover(string filePath, string outputDirectory)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Extracting cover art... ");

            var coverFile = Path.Combine(outputDirectory, "cover.jpg");
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-audible_key \"{_key}\" ")
                .AddParameter($"-audible_iv \"{_iv}\" ")
                .AddParameter($"-i \"{filePath}\"")
                .AddParameter("-an -vcodec copy")
                .SetOutput(coverFile);

            conversion.Start().Wait();

            logger.WriteLine("Done");
            return coverFile;
        }

        private void AddMetadataToM4A(string filePath, AaxInfoDto? aaxInfo, string coverPath)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Adding metadata to M4A... ");
            
            using (var file = TagLib.File.Create(filePath))
            {
                //var tag = file.GetTag(TagTypes.Apple);
                var tag = file.GetTag(TagTypes.Apple);
                
                bool found = false;
                if (_library != null)
                {
                    _library = _library.OrderBy(x => x.authors).ToList();
                    var regex = new Regex("Part [0-9]");
                    var boxset = regex.Match(aaxInfo?.format.tags.title).Success;
                    var titleString = boxset ? Regex.Replace(aaxInfo?.format.tags.title, @"\bPart [1-9]\b", "").Trim(): aaxInfo?.format.tags.title;
                    var data = _library.FirstOrDefault(x => titleString == x.title);
                    if (data != null)
                    {
                        found = true;

                        tag.Copyright = aaxInfo.format.tags.copyright;
                        tag.AlbumArtists = data.authors.Split(",");
                        tag.Title = data.title;
                        tag.Album = data.title;
                        tag.Subtitle = data.subtitle;
                        tag.Year = (uint) data.release_date.Year;
                        tag.Composers = data.narrators.Split(",");
                        tag.Description = data.extended_product_description;
                        tag.Genres = data.genres.Split(",");
                        tag.AmazonId = data.asin;
                        
                    }
                }
                
                if (!found)
                {
                    tag.Title = aaxInfo?.format?.tags?.title;
                    tag.Performers = new[] { aaxInfo?.format?.tags?.artist};
                    tag.Album = aaxInfo?.format?.tags?.album;
                    if (aaxInfo?.format?.tags?.date != null)
                        tag.Year = uint.Parse(aaxInfo.format.tags.date);
                    tag.Genres = new[] { aaxInfo?.format?.tags?.genre };
                    tag.Copyright = aaxInfo?.format?.tags?.copyright;
                }
                if (File.Exists(coverPath))
                {
                    var coverPicture = new Picture(coverPath)
                    {
                        Type = PictureType.FrontCover,
                        Description = "Cover",
                        MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
                    };
                    tag.Pictures = new IPicture[] { coverPicture };
                }
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
            
        }
    }
}