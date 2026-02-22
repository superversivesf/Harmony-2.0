using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Harmony.Dto;
using Microsoft.VisualBasic.FileIO;
using TagLib;
using Xabe.FFmpeg;
using File = System.IO.File;
using SearchOption = System.IO.SearchOption;

namespace Harmony
{
    internal class AaxToM4BConvertor
    {
        /// <summary>
        /// Maximum number of authors to display individually before showing "Various".
        /// </summary>
        public const int MaxAuthorCountForIndividualDisplay = 4;

        private readonly string _activationBytes;
        private readonly int _bitrate;
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly bool _quietMode;
        private List<AudibleLibraryDto>? _library;
        private readonly bool _clobber;

        public AaxToM4BConvertor(string activationBytes, int bitrate, bool quietMode, string inputFolder,
            string outputFolder, bool clobber, List<AudibleLibraryDto>? library = null)
        {
            _clobber = clobber;
            _activationBytes = activationBytes;
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
            var filePaths = Directory.GetFiles(_inputFolder, "*.aax", SearchOption.AllDirectories);
            logger.WriteLine($"Found {filePaths.Length} aax files to process\n");
            foreach (var filePath in filePaths)
            {
                ProcessAaxFile(filePath);
            }
        }

        private void ProcessAaxFile(string filePath)
        {
            var logger = new Logger(_quietMode);

            logger.WriteLine($"Processing {Path.GetFileName(filePath)} ...");

            var aaxInfo = GetAaxInfo(filePath);
            WriteAaxInfo(aaxInfo, logger);

            var regex = new Regex("Part_[0-9]-LC");
            var boxset = regex.Match(filePath).Success;
            var outputDirectory = PrepareOutputDirectory(aaxInfo, boxset);
            var intermediateFile = ProcessToM4A(aaxInfo, filePath, outputDirectory);

            if (intermediateFile is null)
                return;

            var m4BFilePath = intermediateFile.Replace(".m4a", "-nochapter.m4b");
            var coverFile = GenerateCover(filePath, outputDirectory);
            var chapterFile = Path.Combine(outputDirectory, "chapter.txt");
            ChapterConverter.CreateChapterFile(filePath, aaxInfo, chapterFile);

            AddMetadataToM4A(intermediateFile, aaxInfo, coverFile);

            File.Move(intermediateFile, m4BFilePath);

            var analyser = new FFProbeAnalyzer();

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

            var outputFile = Path.Combine(outputDirectory, m4BFilePath.Replace("-nochapter", string.Empty));
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
            if (aaxInfo?.format?.duration is not null)
                logger.WriteLine(
                    $"Length: {TimeSpan.FromSeconds(double.Parse(aaxInfo.format.duration)):hh\\:mm\\:ss}");
            if (aaxInfo?.chapters is not null) logger.WriteLine($"Chapters: {aaxInfo.chapters.Count}");
        }

        private string PrepareOutputDirectory(AaxInfoDto? aaxInfo, bool boxset)
        {
            var title = CleanTitle(aaxInfo?.format?.tags?.title);
            if (boxset)
            {
                title = Regex.Replace(title, @"\bPart [1-9]\b", "").Trim();
            }

            var author = CleanAuthor(aaxInfo?.format?.tags?.artist);
            var outputDirectory = Path.Combine(_outputFolder, author, title);
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
                .AddParameter($"-activation_bytes \"{_activationBytes}\" ")
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

            if (File.Exists(m4BFilePath) && _clobber)
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
                .AddParameter($"-activation_bytes \"{_activationBytes}\" ")
                .AddParameter($"-i \"{filePath}\" ")
                .AddParameter("-map_chapters -1 -vn -codec:a copy -b:a 64k ")
                .SetOutput(outputFile);

            //conversion.OnProgress += ProgressMeter;
            conversion.Start().Wait();
            Console.WriteLine();

            return outputFile;
        }

        private string GenerateCover(string filePath, string outputDirectory)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Extracting cover art... ");

            var directory = Path.GetDirectoryName(filePath);
            var filename = Path.GetFileName(filePath);
            var baseName = filename.Split("-AAX")[0];
            var pattern = $@"^{Regex.Escape(baseName)}_\(\d+\)\.jpg$";
            var matchingFile = directory is not null
                ? Directory.GetFiles(directory, "*.jpg")
                    .Where(f => Regex.IsMatch(Path.GetFileName(f), pattern, RegexOptions.IgnoreCase))
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Length)
                    .Select(f => f.Name).FirstOrDefault()
                : null;

            if (matchingFile is not null && directory is not null)
            {
                logger.WriteLine("Done");
                return Path.Combine(directory, matchingFile);
            }

            var coverFile = Path.Combine(outputDirectory, "Cover.jpg");
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-activation_bytes {_activationBytes}")
                .AddParameter($"-i \"{filePath}\"")
                .AddParameter("-an -vcodec copy")
                .SetOutput(coverFile);

            conversion.Start().Wait();

            logger.WriteLine("Done");
            return coverFile;
        }

        string ProcessTitleForComparison(string input)
        {
            return Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]", "");
        }

        private void AddMetadataToM4A(string filePath, AaxInfoDto? aaxInfo, string coverPath)
        {
            var logger = new Logger(_quietMode);
            logger.Write("Adding metadata to M4A... ");

            using (var file = TagLib.File.Create(filePath))
            {
                var tag = file.GetTag(TagTypes.Apple);

                var found = false;
                if (_library is not null)
                {
                    var lookupTable = _library
                        .GroupBy(dto => ProcessTitleForComparison(dto.title + dto.subtitle))
                        .ToDictionary(
                            group => group.Key,
                            group => group.ToList()
                        );


                    var title = aaxInfo?.format?.tags?.title;
                    if (title is null)
                    {
                        found = false;
                    }
                    else
                    {
                        var regex = new Regex("Part [0-9]");
                        var boxset = regex.Match(title).Success;
                        var titleString = boxset ? Regex.Replace(title, @"\bPart [1-9]\b", "").Trim() : title;

                        string processedTitleString = ProcessTitleForComparison(titleString);

                        lookupTable.TryGetValue(processedTitleString, out var dataList);

                        var data = dataList?.Count == 1 ? dataList[0] : null;

                        if (dataList?.Count > 1)
                            logger.WriteLine($"Duplicate Title Found -> {titleString}");

                        if (data is not null)
                        {
                            found = true;
                            tag.Copyright = aaxInfo?.format?.tags?.copyright;
                            tag.AlbumArtists = data.authors?.Split(",") ?? Array.Empty<string>();
                            tag.Title = data.title;
                            tag.Album = data.title;
                            tag.Subtitle = data.subtitle;
                            tag.Year = (uint)data.release_date.Year;
                            tag.Composers = data.narrators?.Split(",") ?? Array.Empty<string>();
                            tag.Description = data.extended_product_description;
                            tag.Genres = data.genres?.Split(",") ?? Array.Empty<string>();
                            tag.AmazonId = data.asin;
                        }

                        var metadata = new AbsMetadata();

                        metadata.title = data?.title;
                        metadata.subtitle = data?.subtitle;
                        metadata.authors = data?.authors?.Split(",").Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                        metadata.narrators = data?.narrators?.Split(",").Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                        metadata.series = !string.IsNullOrWhiteSpace(data?.series_title)
                            ? new List<string> { $"{data.series_title} #{data?.series_sequence}".Trim() }
                            : new List<string>();
                        metadata.genres = data?.genres?.Split(',').Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                        metadata.publishedYear = data?.release_date.Year.ToString();
                        metadata.publishedDate = data?.release_date.Date.ToString("yyyy-MM-dd");
                        metadata.description = data?.extended_product_description;
                        metadata.asin = data?.asin;

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        var metadataString = JsonSerializer.Serialize(metadata, options);
                        var metadataFile = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, "metadata.json");
                        File.WriteAllText(metadataFile, metadataString);
                    }
                }

                if (!found)
                {
                    tag.Title = aaxInfo?.format?.tags?.title;
                    tag.Performers = new[] { aaxInfo?.format?.tags?.artist };
                    tag.Album = aaxInfo?.format?.tags?.album;
                    if (aaxInfo?.format?.tags?.date is not null)
                        tag.Year = uint.Parse(aaxInfo.format.tags.date);
                    tag.Genres = new[] { aaxInfo?.format?.tags?.genre };
                    tag.Copyright = aaxInfo?.format?.tags?.copyright;


                    var metadata = new AbsMetadata();

                    metadata.title = aaxInfo?.format?.tags?.title;
                    metadata.authors = aaxInfo?.format?.tags?.artist?.Split(",").ToList();

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var metadataString = JsonSerializer.Serialize(metadata, options);
                    var metadataFile = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, "metadata.json");
                    File.WriteAllText(metadataFile, metadataString);

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
            if (authors.Length > MaxAuthorCountForIndividualDisplay) return "Various";

            return name.Replace("Jr.", "Jr").Trim();
        }

        private void CheckFolders()
        {
            if (!Directory.Exists(_inputFolder)) throw new Exception($"Input folder does not exist: {_inputFolder}");
            if (!Directory.Exists(_outputFolder)) throw new Exception($"Output folder does not exist: {_outputFolder}");

        }
    }
}