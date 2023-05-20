using System.Text.Json;
using Audio_Convertor;
using Audio_Convertor.AudioJson;
using Audio_Convertor.ChaptersJson;
using Audio_Convertor.StreamsJson;
using FFMpegCore;
using FFMpegCore.Helpers;
using Instances;
using TagLib;
using Xabe.FFmpeg.Downloader;

namespace Harmony
{
    class AaxAudioConvertor
    {
        private readonly string _activationBytes;
        private readonly int _bitrate;
        private readonly bool _quietMode;
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly string _storageFolder;
        private readonly string _workingFolder;

        public AaxAudioConvertor(string activationBytes, int bitrate, bool quietMode, string inputFolder, string outputFolder, string storageFolder, string workingFolder)
        {
            this._activationBytes = activationBytes;
            this._bitrate = bitrate;
            this._quietMode = quietMode;
            this._inputFolder = inputFolder;
            this._outputFolder = outputFolder;
            this._storageFolder = storageFolder;
            this._workingFolder = workingFolder;
        }

        internal void Execute()
        {
            var logger = new Logger(_quietMode);

            GlobalFFOptions.Configure(new FFOptions { WorkingDirectory = ".", TemporaryFilesFolder = "." });
            logger.Write("Fetching Latest FFMpeg ...  ");

            var fetchTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            while (!fetchTask.IsCompleted)
            {
                logger.AdvanceSpinnder();
                Thread.Sleep(50);
            }

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
            foreach (var f in filePaths)
            {
                ProcessAaxFile(f);
            }
        }

        private void ProcessAaxFile(string f)
        {
            var logger = new Logger(_quietMode);
            var storageFolder = this._storageFolder;

            var aaxInfo = GetAAXInfo(f);

            // Write out relevant stats
            logger.WriteLine($"Title: {CleanTitle(aaxInfo.Format.format.tags.title)}");
            logger.WriteLine($"Author(s): {aaxInfo.Format.format.tags.artist}");

            double duration = Double.Parse(aaxInfo.Format.format.duration);
            int h = (int)duration / 3600;
            int m = ((int)duration - h * 3600) / 60;
            int s = ((int)duration - h * 3600 - m * 60);

            logger.WriteLine($"Length: {h:D2}:{m:D2}:{s:D2}");
            logger.WriteLine($"Chapters: {aaxInfo.Chapters.chapters.Count}");

            var intermediateFile = ProcessToMp3(f, aaxInfo);
            var coverFile = GenerateCover(f);
            var outputDirectory = ProcessChapters(intermediateFile, aaxInfo, coverFile);

            logger.Write("Moving Cover file ... ");
            var coverFileDestination = Path.Combine(outputDirectory, "Cover.jpg");
            System.IO.File.Move(coverFile, coverFileDestination);
            logger.WriteLine("Done");

            logger.Write("Moving AAX file to storage ... ");
            var storageFile = Path.Combine(storageFolder, Path.GetFileName(f));
            System.IO.File.Move(f, storageFile);
            logger.WriteLine("Done");

            // Cleanup 
            logger.Write("Cleaning up intermediate files ... ");
            System.IO.File.Delete(intermediateFile);
            logger.WriteLine("Done\n");

            //Console.WriteLine(instance.OutputData);
            //// https://github.com/inAudible-NG/tables

        }

        private void PurgeOutputDirectory(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(outputDirectory);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
        }

        private string GenerateCover(string f)
        {
            var logger = new Logger(_quietMode);
            var filePath = f;
            var activationBytes = this._activationBytes;

            var ffmpeg = GlobalFFOptions.GetFFMpegBinaryPath();

            var coverFile = Path.Combine(_workingFolder, "Cover.jpg");

            logger.Write("Writing Cover File ... ");

            var arguments = $"-activation_bytes {activationBytes} -i \"{filePath}\" -an -codec:v copy \"{coverFile}\"";
            var instance = new Instance(ffmpeg, arguments);
            instance.BlockUntilFinished();

            logger.WriteLine("Done");

            return coverFile;
        }

        private string ProcessChapters(string filepath, AAXInfo aaxInfo, string coverPath)
        {
            var logger = new Logger(_quietMode);
            var activationBytes = this._activationBytes;
            var ffmpeg = GlobalFFOptions.GetFFMpegBinaryPath();
            var outputFolder = this._outputFolder;
            var filePath = filepath;
            var title = CleanTitle(aaxInfo.Format.format.tags.title);
            var author = CleanAuthor(aaxInfo.Format.format.tags.artist);
            var outputDirectory = Path.Combine(outputFolder, author);
            outputDirectory = Path.Combine(outputDirectory, title);
            var m3UFileName = $"{title}.m3u";

            var invalidPathChars = Path.GetInvalidPathChars();
            foreach (var c in invalidPathChars)
            {
                outputDirectory = outputDirectory.Replace(c, '_');
            }

            PurgeOutputDirectory(outputDirectory);

            var directoryInfo = Directory.CreateDirectory(outputDirectory);
            var m3UFilePath = Path.Combine(outputDirectory, m3UFileName);
            var m3UFile = new StreamWriter(m3UFilePath);
            var chapterCount = aaxInfo.Chapters.chapters.Count;
            var formatString = "";

            if (chapterCount > 100)
            {
                formatString = "D3";
            }
            else if (chapterCount > 10)
            {
                formatString = "D2";
            }
            else
            {
                formatString = "D1";
            }

            logger.WriteLine($"Processing {title} with {chapterCount} Chapters");

            InitM3U(m3UFile);

            foreach (var c in aaxInfo.Chapters.chapters)
            {
                var startChapter = c.start_time;
                var endChapter = c.end_time;
                var chapterNumber = c.id + 1; // zero based
                var chapterFileTitle = c.tags.title.Trim();
                var chapterFile = title + "-" + chapterNumber.ToString(formatString) + "-" + chapterFileTitle + ".mp3";
                var chapterFilePath = Path.Combine(outputDirectory, chapterFile);
                logger.Write($"\rWriting Chapter {c.id + 1} ...  ");

                var arguments = $" -i \"{filePath}\" -ss \"{startChapter}\" -to \"{endChapter}\" -acodec mp3 \"{chapterFilePath}\"";
                var instance = new Instance(ffmpeg, arguments) { DataBufferCapacity = int.MaxValue };
                var encodeTask = instance.FinishedRunning();
                while (!encodeTask.IsCompleted)
                {
                    logger.AdvanceSpinnder();
                    Thread.Sleep(100);
                }

                // Encode MP3 tags and cover here // write m3u file as well at the same time

                UpdateM3UAndTagFile(m3UFile, chapterFilePath, aaxInfo, coverPath, c);

                logger.WriteLine("\bDone");
            }
            m3UFile.Close();
            return outputDirectory;
        }

        private void InitM3U(StreamWriter m3UFile)
        {
            m3UFile.WriteLine("# EXTM3U");
        }

        private void UpdateM3UAndTagFile(StreamWriter m3UFile, string chapterFile, AAXInfo aaxInfo, string coverPath, Chapter chapter)
        {
            var tagFile = TagLib.File.Create(chapterFile);
            var title = CleanTitle(aaxInfo.Format.format.tags.title);
            m3UFile.WriteLine($"# EXTINF:{tagFile.Properties.Duration.TotalSeconds.ToString("0F")},{aaxInfo.Format.format.tags.title} - {chapter.tags.title}");
            m3UFile.WriteLine(Path.GetFileName(chapterFile));

            var coverPicture = new TagLib.PictureLazy(coverPath);
            tagFile.Tag.Pictures = new IPicture[] { coverPicture };

            tagFile.Tag.Title = title + " - " + chapter.tags.title;
            tagFile.Tag.AlbumArtists = new string[] { aaxInfo.Format.format.tags.artist };
            tagFile.Tag.Album = title;
            tagFile.Tag.Track = (uint)chapter.id + 1;
            tagFile.Tag.TrackCount = (uint)aaxInfo.Chapters.chapters.Count;

            tagFile.Tag.Copyright = aaxInfo.Format.format.tags.copyright;
            tagFile.Tag.DateTagged = aaxInfo.Format.format.tags.creation_time;
            tagFile.Tag.Comment = aaxInfo.Format.format.tags.comment;
            tagFile.Tag.Description = aaxInfo.Format.format.tags.comment;
            tagFile.Tag.Genres = new string[] { aaxInfo.Format.format.tags.genre };
            tagFile.Tag.Publisher = "";
            tagFile.Tag.Year = (uint)aaxInfo.Format.format.tags.creation_time.Year;

            tagFile.Save();

        }

        private string CleanAuthor(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return "Unknown";
            }

            var authors = name.Split(',');
            if (authors.Count() > 4)
            {
                return ("Various");
            }

            return name.Replace("Jr.", "Jr").Trim();
        }

        private string CleanTitle(string title)
        {
            return title.Replace("(Unabridged)", String.Empty).Replace(":", " -").Replace("'", String.Empty).Replace("?", String.Empty).Trim();
        }

        private string ProcessToMp3(string filePath, AAXInfo aaxInfo)
        {
            var logger = new Logger(_quietMode);
            var activationBytes = this._activationBytes;
            var bitrate = this._bitrate;

            var ffmpeg = GlobalFFOptions.GetFFMpegBinaryPath();

            var intermediateFile = Path.GetFileName(filePath);
            intermediateFile = Path.ChangeExtension(intermediateFile, "mp3");
            intermediateFile = Path.Combine(_workingFolder, intermediateFile);

            logger.Write("Recoding to mp3 ...  ");

            var arguments = $"-activation_bytes {activationBytes} -i \"{_filePath}\" -vn -codec:a mp3 -ab {bitrate}k \"{intermediateFile}\"";
            var instance = new Instance(ffmpeg, arguments) { DataBufferCapacity = int.MaxValue };
            var encodeTask = instance.FinishedRunning();
            while (!encodeTask.IsCompleted)
            {
                logger.AdvanceSpinnder();
                Thread.Sleep(100);
            }

            logger.WriteLine("\bDone");

            return intermediateFile;

        }

        private AAXInfo GetAAXInfo(string f)
        {
            var _activationBytes = this._activationBytes;
            var logger = new Logger(_quietMode);

            FFProbeHelper.RootExceptionCheck(GlobalFFOptions.Options.RootDirectory);
            var filePath = f;
            var ffprobe = GlobalFFOptions.GetFFProbeBinaryPath();

            logger.Write("Probing ");

            var arguments = $"-print_format json -activation_bytes {this._activationBytes} -show_format \"{filePath}\"";
            var instance = new Instance(ffprobe, arguments) { DataBufferCapacity = int.MaxValue };
            instance. BlockUntilFinished();
            var formatJson = string.Join(string.Empty, instance.OutputData);

            logger.Write(".");

            arguments = $"-print_format json -activation_bytes {this._activationBytes} -show_streams \"{filePath}\"";
            instance = new Instance(ffprobe, arguments) { DataBufferCapacity = int.MaxValue };
            instance.BlockUntilFinished();
            var streamsJson = string.Join(string.Empty, instance.OutputData);

            logger.Write(".");

            arguments = $"-print_format json -activation_bytes {this._activationBytes} -show_chapters \"{filePath}\"";
            instance = new Instance(ffprobe, arguments) { DataBufferCapacity = int.MaxValue };
            instance.BlockUntilFinished();
            var chaptersJson = string.Join(string.Empty, instance.OutputData);

            logger.Write(".");

            var audioFormat = JsonSerializer.Deserialize<AudioFormat>(formatJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var audioChapters = JsonSerializer.Deserialize<AudioChapters>(chaptersJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var audioStreams = JsonSerializer.Deserialize<AudioStreams>(streamsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = new AAXInfo(audioFormat, audioChapters, audioStreams);

            logger.WriteLine(" Done");

            return result;
        }

        private void CheckFolders()
        {
            if (!Directory.Exists(_inputFolder))
            {
                throw new Exception("Input folder does not exist: " + _inputFolder);
            }

            if (!Directory.Exists(_outputFolder))
            {
                throw new Exception("Output folder does not exist: " + _inputFolder);
            }

            if (!Directory.Exists(_storageFolder))
            {
                throw new Exception("Storage folder does not exist: " + _inputFolder);
            }

            if (!Directory.Exists(_workingFolder))
            {
                throw new Exception("Working folder does not exist: " + _inputFolder);
            }

            System.IO.DirectoryInfo di = new DirectoryInfo(_workingFolder);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
