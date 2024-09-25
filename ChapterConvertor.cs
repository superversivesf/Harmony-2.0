using System.Text;

namespace Harmony;


public class ChapterConverter
{
    public static void CreateChapterFile(AaxInfoDto aaxinfo, string outputPath)
    {
        var sb = new StringBuilder();
       sb.AppendLine(";FFMETADATA1");
        
        for (int i = 0; i < aaxinfo.chapters.Count; i++)
        {
            var chapter = aaxinfo.chapters[i];
            double startTime = double.Parse(chapter.start_time) * 1000.0 ;
            double endTime = (double.Parse(chapter.end_time) * 1000.0) - 1.0;

            sb.AppendLine("[CHAPTER]");
            sb.AppendLine("TIMEBASE=1/1000");
            sb.AppendLine("START=" + startTime.ToString("F0"));
            sb.AppendLine("END=" + endTime.ToString("F0"));
            sb.AppendLine("title=Chapter " + (i + 1));
        }

        File.WriteAllText(outputPath, sb.ToString());
    }
}