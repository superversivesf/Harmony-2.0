using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Harmony.Dto;
using Spectre.Console;
using TagLib;
using Xabe.FFmpeg;
using File = System.IO.File;
using JsonSerializer = System.Text.Json.JsonSerializer;
using SearchOption = System.IO.SearchOption;

namespace Harmony;

/// <summary>
/// Abstract base class for audiobook converters. Provides shared functionality for
/// converting AAX/AAXC files to M4B format while allowing subclasses to specify
/// authentication parameters.
/// </summary>
internal abstract class AudiobookConverterBase
{
    /// <summary>
    /// Maximum number of authors to display individually before showing "Various".
    /// </summary>
    public const int MaxAuthorCountForIndividualDisplay = 4;

    /// <summary>
    /// Default AAC bitrate used for encoding when copying is not possible.
    /// </summary>
    internal const string DefaultAacBitrate = "64k";

    private readonly int _bitrate;
    private readonly string _inputFolder;
    private readonly string _outputFolder;
    private readonly bool _quietMode;
    private List<AudibleLibraryDto>? _library;
    private readonly bool _clobber;
    private readonly ProgressContextManager? _progressManager;

    protected AudiobookConverterBase(int bitrate, bool quietMode, string inputFolder,
        string outputFolder, bool clobber, List<AudibleLibraryDto>? library = null,
        ProgressContextManager? progressManager = null)
    {
        _clobber = clobber;
        _bitrate = bitrate;
        _quietMode = quietMode;
        _inputFolder = inputFolder;
        _outputFolder = outputFolder;
        _library = library;
        _progressManager = progressManager;
    }

    /// <summary>
    /// Gets the file extension pattern to search for (e.g., "*.aax" or "*.aaxc").
    /// </summary>
    protected abstract string FileExtensionPattern { get; }

    /// <summary>
    /// Gets the authentication parameters for FFmpeg commands.
    /// For AAX: returns ["-activation_bytes", "{bytes}"]
    /// For AAXC: returns ["-audible_key", "{key}", "-audible_iv", "{iv}"]
    /// </summary>
    protected abstract IEnumerable<string> GetAuthenticationParameters();

    /// <summary>
    /// Gets file info by probing the input file with authentication.
    /// </summary>
    protected abstract AaxInfoDto? GetFileInfo(string filePath);

    /// <summary>
    /// Executes the conversion process for all files in the input folder.
    /// </summary>
    internal async Task ExecuteAsync()
    {
        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        var logger = new Logger(_quietMode, _progressManager is not null);
        logger.WriteLine("\bDone");

        logger.Write("Checking folders and purging working files ... ");
        CheckFolders();
        logger.WriteLine("Done");
        var filePaths = Directory.GetFiles(_inputFolder, FileExtensionPattern, SearchOption.AllDirectories);
        logger.WriteLine($"Found {filePaths.Length} files to process\n");

        foreach (var filePath in filePaths)
        {
            if (_progressManager?.IsCancelled == true)
                throw new OperationCanceledException();
            _progressManager?.StartNewFile(Path.GetFileName(filePath));

            await ProcessFileAsync(filePath).ConfigureAwait(false);

            _progressManager?.CompleteFile();
        }
    }

    private async Task ProcessFileAsync(string filePath)
    {
        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        var logger = new Logger(_quietMode, _progressManager is not null);

        if (!logger.IsTuiMode)
            logger.WriteLine($"Processing {Path.GetFileName(filePath)} ...");

        var aaxInfo = GetFileInfo(filePath);
        if (aaxInfo is null)
        {
            if (!logger.IsTuiMode)
                logger.WriteLine("Failed to get file info, skipping file.");
            return;
        }

        if (!logger.IsTuiMode)
            WriteFileInfo(aaxInfo, logger);

        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        var regex = new Regex("Part_[0-9]-LC");
        var boxset = regex.Match(filePath).Success;
        var outputDirectory = PrepareOutputDirectory(aaxInfo, boxset);
        var intermediateFile = await ProcessToM4AAsync(aaxInfo, filePath, outputDirectory).ConfigureAwait(false);

        if (intermediateFile is null)
            return;

        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        var m4BFilePath = intermediateFile.Replace(".m4a", "-nochapter.m4b");
        var coverFile = await GenerateCoverAsync(filePath, outputDirectory).ConfigureAwait(false);
        var chapterFile = Path.Combine(outputDirectory, "chapter.txt");
        ChapterConverter.CreateChapterFile(filePath, aaxInfo, chapterFile);

        AddMetadataToM4A(intermediateFile, aaxInfo, coverFile);

        File.Move(intermediateFile, m4BFilePath);

        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        var analyser = new FFProbeAnalyzer(logger);

        var goodFileCheck = await analyser.AnalyzeFile(m4BFilePath).ConfigureAwait(false);

        if (!goodFileCheck)
        {
            logger.WriteLine("Problem with file, trying fallback processing");
            File.Delete(m4BFilePath);
            outputDirectory = PrepareOutputDirectory(aaxInfo, boxset);
            intermediateFile = await FallbackProcessToM4AAsync(aaxInfo, filePath, outputDirectory).ConfigureAwait(false);
            coverFile = await GenerateCoverAsync(filePath, outputDirectory).ConfigureAwait(false);

            AddMetadataToM4A(intermediateFile, aaxInfo, coverFile);

            m4BFilePath = intermediateFile.Replace(".m4a", "-nochapter.m4b");
            File.Move(intermediateFile, m4BFilePath);
        }

        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        logger.Write("Adding Chapters ...      ");

        var outputFile = Path.Combine(outputDirectory, m4BFilePath.Replace("-nochapter", string.Empty));
        var conversion = FFmpeg.Conversions.New()
            .AddParameter("-i")
            .AddParameter($"\"{m4BFilePath}\"")
            .AddParameter("-i")
            .AddParameter($"\"{chapterFile}\"")
            .AddParameter("-map_metadata")
            .AddParameter("1")
            .AddParameter("-codec")
            .AddParameter("copy")
            .SetOutput($"\"{outputFile}\"");

        await conversion.Start().ConfigureAwait(false);

        logger.WriteLine("Done");

        File.Delete(chapterFile);
        File.Delete(m4BFilePath);

        logger.WriteLine($"Successfully converted {Path.GetFileName(filePath)} to M4B.");
    }

    private void WriteFileInfo(AaxInfoDto? aaxInfo, Logger logger)
    {
        logger.WriteLine($"Title: {CleanTitle(aaxInfo?.format?.tags?.title)}");
        logger.WriteLine($"Author(s): {aaxInfo?.format?.tags?.artist}");
        if (aaxInfo?.format?.duration is not null)
        {
            if (double.TryParse(aaxInfo.format.duration, out var duration))
                logger.WriteLine($"Length: {TimeSpan.FromSeconds(duration):hh\\:mm\\:ss}");
            else
                logger.WriteLine("Warning: Invalid duration format in file metadata");
        }
        if (aaxInfo?.chapters is not null) logger.WriteLine($"Chapters: {aaxInfo.chapters.Count}");
    }

    private string PrepareOutputDirectory(AaxInfoDto? aaxInfo, bool boxset)
    {
        var title = CleanTitle(aaxInfo?.format?.tags?.title) ?? "Unknown";
        if (boxset)
        {
            title = Regex.Replace(title, @"\bPart [1-9]\b", "").Trim();
        }

        var author = CleanAuthor(aaxInfo?.format?.tags?.artist);
        var outputDirectory = Path.Combine(_outputFolder, author, title);
        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    private async Task<string> FallbackProcessToM4AAsync(AaxInfoDto aaxInfo, string filePath, string outputDirectory)
    {
        var logger = new Logger(_quietMode, _progressManager is not null);

        logger.Write("Converting to WAV ...      ");

        var title = CleanTitle(aaxInfo.format?.tags?.title);
        var filename = title;
        var outputFile = Path.Combine(outputDirectory, filename + ".wav");
        var authParams = GetAuthenticationParameters();
        var conversion = FFmpeg.Conversions.New();
        
        // Add authentication parameters safely - each parameter is added individually
        foreach (var param in authParams)
        {
            conversion.AddParameter(param);
        }
        
        conversion
            .AddParameter("-i")
            .AddParameter($"\"{filePath}\"")
            .AddParameter("-vn")
            .AddParameter("-codec:a")
            .AddParameter("pcm_s16le")
            .AddParameter("-q:a")
            .AddParameter("0")
            .SetOutput($"\"{outputFile}\"");

        await conversion.Start().ConfigureAwait(false);
        if (_progressManager == null)
            Console.WriteLine();
        logger.Write("Converting WAV to M4A...       ");

        filePath = outputFile;
        outputFile = Path.Combine(outputDirectory, filename + ".m4a");
        conversion = FFmpeg.Conversions.New()
            .AddParameter("-i")
            .AddParameter($"\"{filePath}\"")
            .AddParameter("-vn")
            .AddParameter("-codec:a")
            .AddParameter("aac")
            .AddParameter("-b:a")
            .AddParameter($"{_bitrate}k")
            .SetOutput($"\"{outputFile}\"");

        await conversion.Start().ConfigureAwait(false);

        File.Delete(filePath);
        if (_progressManager == null)
            Console.WriteLine();

        return outputFile;
    }

    private async Task<string?> ProcessToM4AAsync(AaxInfoDto aaxInfo, string filePath, string outputDirectory)
    {
        var logger = new Logger(_quietMode, _progressManager is not null);
        logger.Write("Converting to M4A...      ");

        var title = CleanTitle(aaxInfo.format?.tags?.title);
        var filename = title;
        var outputFile = Path.Combine(outputDirectory, filename + ".m4a");

        var m4BFilePath = Path.ChangeExtension(outputFile, "m4b");

        // Handle existing M4B file
        if (File.Exists(m4BFilePath))
        {
            if (_clobber)
            {
                logger.WriteLine("\nFile Already Exists ... Deleting");
                File.Delete(m4BFilePath);
            }
            else
            {
                logger.WriteLine("\nFile Already Exists ... Skipping");
                return null;
            }
        }
        
        // Clean up intermediate file if it exists
        if (File.Exists(outputFile))
        {
            try
            {
                File.Delete(outputFile);
            }
            catch (IOException ex)
            {
                logger.WriteLine($"\nWarning: Could not delete intermediate file {outputFile}: {ex.Message}");
            }
        }
        
        // Clean up intermediate file if it exists
        if (File.Exists(outputFile))
        {
            try
            {
                File.Delete(outputFile);
            }
            catch (IOException ex)
            {
                logger.WriteLine($"\nWarning: Could not delete intermediate file {outputFile}: {ex.Message}");
            }
        }

        var authParams = GetAuthenticationParameters();
        var conversion = FFmpeg.Conversions.New();
        
        // Add authentication parameters safely - each parameter is added individually
        foreach (var param in authParams)
        {
            conversion.AddParameter(param);
        }
        
        conversion
            .AddParameter("-i")
            .AddParameter($"\"{filePath}\"")
            .AddParameter("-map_chapters")
            .AddParameter("-1")
            .AddParameter("-vn")
            .AddParameter("-codec:a")
            .AddParameter("copy")
            .AddParameter("-b:a")
            .AddParameter(DefaultAacBitrate)
            .SetOutput($"\"{outputFile}\"");

        await conversion.Start().ConfigureAwait(false);
        if (_progressManager == null)
            Console.WriteLine();

        return outputFile;
    }

    private async Task<string> GenerateCoverAsync(string filePath, string outputDirectory)
    {
        var logger = new Logger(_quietMode, _progressManager is not null);
        logger.Write("Extracting cover art... ");

        var directory = Path.GetDirectoryName(filePath);
        var coverFile = Path.Combine(outputDirectory, "Cover.jpg");

        // Look for existing cover file
        if (directory is not null)
        {
            var filename = Path.GetFileName(filePath);
            var baseName = filename.Split("-AAX")[0];
            var pattern = $"^{Regex.Escape(baseName)}_\\(\\d+\\)\\.jpg$";
            var matchingFile = Directory.GetFiles(directory, "*.jpg")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), pattern, RegexOptions.IgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.Length)
                .Select(f => f.Name).FirstOrDefault();

            if (matchingFile is not null)
            {
                logger.WriteLine("Done");
                return Path.Combine(directory, matchingFile);
            }
        }

        var authParams = GetAuthenticationParameters();
        var conversion = FFmpeg.Conversions.New();
        
        // Add authentication parameters safely - each parameter is added individually
        foreach (var param in authParams)
        {
            conversion.AddParameter(param);
        }
        
        conversion
            .AddParameter("-i")
            .AddParameter($"\"{filePath}\"")
            .AddParameter("-an")
            .AddParameter("-vcodec")
            .AddParameter("copy")
            .SetOutput($"\"{coverFile}\"");

        await conversion.Start().ConfigureAwait(false);

        logger.WriteLine("Done");
        return coverFile;
    }

    private void AddMetadataToM4A(string filePath, AaxInfoDto? aaxInfo, string coverPath)
    {
        if (_progressManager?.IsCancelled == true)
            throw new OperationCanceledException();

        var logger = new Logger(_quietMode, _progressManager is not null);
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
                    if (title is not null)
                    {
                        var regex = new Regex("Part [0-9]");
                        var boxset = regex.Match(title).Success;
                        var titleString = boxset ? Regex.Replace(title, @"\bPart [1-9]\b", "").Trim() : title;
                        titleString = Regex.Replace(titleString, @"\(Unabridged\)", "").Trim();

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

                        var metadata = new AbsMetadata
                        {
                            title = data.title,
                            subtitle = data.subtitle,
                            authors = data.authors?.Split(",").Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).ToList(),
                            narrators = data.narrators?.Split(",").Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).ToList(),
                            series = !string.IsNullOrWhiteSpace(data.series_title)
                                ? new List<string> { $"{data.series_title} #{data.series_sequence}".Trim() }
                                : new List<string>(),
                            genres = data.genres?.Split(',').Select(g => g.Trim()).Where(g => !string.IsNullOrWhiteSpace(g)).ToList(),
                            publishedYear = data.release_date.Year.ToString(),
                            publishedDate = data.release_date.Date.ToString("yyyy-MM-dd"),
                            description = data.extended_product_description,
                            asin = data.asin
                        };

                        var options = new JsonSerializerOptions { WriteIndented = true };
                        var metadataString = JsonSerializer.Serialize(metadata, options);
                        var metadataFile = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, "metadata.json");
                        File.WriteAllText(metadataFile, metadataString);
                    }
                }
            }

            if (!found)
            {
                tag.Title = aaxInfo?.format?.tags?.title;
                tag.Performers = new[] { aaxInfo?.format?.tags?.artist };
                tag.Album = aaxInfo?.format?.tags?.album;
                if (aaxInfo?.format?.tags?.date is not null)
                {
                    if (uint.TryParse(aaxInfo.format.tags.date, out var year))
                        tag.Year = year;
                }
                tag.Genres = new[] { aaxInfo?.format?.tags?.genre };
                tag.Copyright = aaxInfo?.format?.tags?.copyright;

                var metadata = new AbsMetadata
                {
                    title = aaxInfo?.format?.tags?.title,
                    authors = aaxInfo?.format?.tags?.artist?.Split(",").ToList()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var metadataString = JsonSerializer.Serialize(metadata, options);
                var metadataFile = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, "metadata.json");
                File.WriteAllText(metadataFile, metadataString);
            }

            // Add cover art - TOCTOU-safe: try to create Picture and catch IOException
            try
            {
                var coverPicture = new Picture(coverPath)
                {
                    Type = PictureType.FrontCover,
                    Description = "Cover",
                    MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
                };
                tag.Pictures = new IPicture[] { coverPicture };
            }
            catch (FileNotFoundException)
            {
                // Cover file doesn't exist, skip adding cover art
            }
            catch (IOException ex)
            {
                // File access error - log and skip
                logger.WriteLine($"Warning: Could not load cover art from {coverPath}: {ex.Message}");
            }

            file.Save();
        }

        logger.WriteLine("Done");
    }

    private string ProcessTitleForComparison(string input)
    {
        if (input is null)
            return string.Empty;

        return Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]", "");
    }

    protected string? CleanTitle(string? title)
    {
        return title?.Replace("(Unabridged)", string.Empty).Replace(":", " -").Replace("'", string.Empty)
            .Replace("?", string.Empty).Trim();
    }

    protected string CleanAuthor(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";

        var authors = name.Split(',');
        if (authors.Length > MaxAuthorCountForIndividualDisplay) return "Various";

        return name.Replace("Jr.", "Jr").Trim();
    }

    protected void CheckFolders()
    {
        if (!Directory.Exists(_inputFolder)) throw new Exception($"Input folder does not exist: {_inputFolder}");
        if (!Directory.Exists(_outputFolder)) throw new Exception($"Output folder does not exist: {_outputFolder}");
    }
}
