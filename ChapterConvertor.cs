using System.Text;
using System.Text.Json;
using Harmony.Dto;

namespace Harmony;


public class ChapterConverter
{
    public static void CreateChapterFile(string filePath, AaxInfoDto aaxinfo, string outputPath)
    { 
        var sb = new StringBuilder();
        sb.AppendLine(";FFMETADATA1");

        var inputDirectory = Path.GetDirectoryName(filePath);
        var audibleChapterFile = Path.Combine(inputDirectory, Path.GetFileName(filePath).Split("-LC")[0] + "-chapters.json");

        if (File.Exists(audibleChapterFile))
        {
            var chapterJson = File.ReadAllText(audibleChapterFile);
            var audibleChpaters = JsonSerializer.Deserialize<AudibleChaptersDto>(chapterJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var chapterList =
                AudibleChaptersDto.FlattenChapters(audibleChpaters.content_metadata.chapter_info.chapters);

            foreach (var c in chapterList)
            {
                
                int startTime = c.start_offset_ms.Value;
                int duration = c.length_ms.Value;
                double endTime = startTime + duration - 1;

                sb.AppendLine("[CHAPTER]");
                sb.AppendLine("TIMEBASE=1/1000");
                sb.AppendLine("START=" + startTime.ToString("F0"));
                sb.AppendLine("END=" + endTime.ToString("F0"));
                sb.AppendLine("title=" + c.title);

            }
            
        }
        else
        { 
            for (int i = 0; i < aaxinfo.chapters.Count; i++)
            {
                var chapter = aaxinfo.chapters[i];
                double startTime = double.Parse(chapter.start_time) * 1000.0;
                double endTime = (double.Parse(chapter.end_time) * 1000.0) - 1.0;

                sb.AppendLine("[CHAPTER]");
                sb.AppendLine("TIMEBASE=1/1000");
                sb.AppendLine("START=" + startTime.ToString("F0"));
                sb.AppendLine("END=" + endTime.ToString("F0"));
                sb.AppendLine("title=Chapter " + (i + 1));
            }
        }

        File.WriteAllText(outputPath, sb.ToString());
    }
}