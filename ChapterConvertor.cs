using System.Text;
using System.Text.Json;
using Harmony.Dto;

namespace Harmony;

internal class ChapterConverter
{
    public static void CreateChapterFile(string filePath, AaxInfoDto aaxinfo, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(";FFMETADATA1");

        var inputDirectory = Path.GetDirectoryName(filePath);
        var audibleChapterFile = Path.Combine(inputDirectory ?? string.Empty, Path.GetFileName(filePath)?.Split("-AAX")[0] + "-chapters.json");

        if (File.Exists(audibleChapterFile))
        {
            var chapterJson = File.ReadAllText(audibleChapterFile);
            var audibleChpaters = JsonSerializer.Deserialize<AudibleChaptersDto>(chapterJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var chapterList =
                AudibleChaptersDto.FlattenChapters(audibleChpaters?.content_metadata?.chapter_info?.chapters);

            foreach (var c in chapterList ?? [])
            {
                int startTime = c.start_offset_ms ?? 0;
                int duration = c.length_ms ?? 0;
                double endTime = startTime + duration - 1;

                sb.AppendLine("[CHAPTER]");
                sb.AppendLine("TIMEBASE=1/1000");
                sb.AppendLine($"START={startTime:F0}");
                sb.AppendLine($"END={endTime:F0}");
                sb.AppendLine($"title={c.title}");
            }
        }
        else if (aaxinfo.chapters?.Count > 0)
        {
            for (int i = 0; i < aaxinfo.chapters.Count; i++)
            {
                var chapter = aaxinfo.chapters[i];

                // Validate start_time parsing
                if (!double.TryParse(chapter.start_time, out double startTime))
                {
                    // Skip chapter with invalid start time
                    continue;
                }

                // Validate end_time parsing
                if (!double.TryParse(chapter.end_time, out double endTime))
                {
                    // Skip chapter with invalid end time
                    continue;
                }

                startTime *= 1000.0;
                endTime = (endTime * 1000.0) - 1.0;

                sb.AppendLine("[CHAPTER]");
                sb.AppendLine("TIMEBASE=1/1000");
                sb.AppendLine($"START={startTime:F0}");
                sb.AppendLine($"END={endTime:F0}");
                sb.AppendLine($"title=Chapter {i + 1}");
            }
        }

        File.WriteAllText(outputPath, sb.ToString());
    }
}