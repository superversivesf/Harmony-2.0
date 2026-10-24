using System.Text;
using System.Text.RegularExpressions;

namespace Harmony;

/// <summary>
/// Pure logic for grouping Audible multipart files and combining their chapter timelines.
/// </summary>
internal static class PartMerging
{
    private static readonly Regex PartRegex = new(
        @"^(?<base>.+)_Part_(?<num>[0-9]+)(-LC_[0-9]{1,3}_[0-9]{4,5}_(stereo|mono))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parses an Audible multipart filename. Returns the book base name and the part
    /// number, or null when the file is not a part of a multipart set.
    /// </summary>
    public static (string Base, int PartNumber)? ParsePartFile(string path)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        var m = PartRegex.Match(stem);
        if (!m.Success)
            return null;
        return (m.Groups["base"].Value, int.Parse(m.Groups["num"].Value));
    }

    /// <summary>
    /// Splits source files into multipart groups (parts sorted numerically) and
    /// single-file books. Non-contiguous part sets fall through as singles.
    /// </summary>
    public static ((
        string Base,
        List<(int PartNumber, string Path)> Parts)[] MergeGroups,
        List<string> Singles) GroupParts(IEnumerable<string> files)
    {
        var groups = new Dictionary<string, List<(int, string)>>();
        var singles = new List<string>();
        foreach (var f in files)
        {
            var parsed = ParsePartFile(f);
            if (parsed is null)
            {
                singles.Add(f);
                continue;
            }
            var (baseName, num) = parsed.Value;
            if (!groups.TryGetValue(baseName, out var list))
                groups[baseName] = list = [];
            list.Add((num, f));
        }

        var mergeGroups = new List<(string Base, List<(int, string)> Parts)>();
        foreach (var (baseName, parts) in groups)
        {
            var ordered = parts.OrderBy(p => p.Item1).ToList();
            if (ordered.Select(p => p.Item1).SequenceEqual(Enumerable.Range(1, ordered.Count)))
                mergeGroups.Add((baseName, ordered));
            else
                singles.AddRange(ordered.Select(p => p.Item2));
        }
        return (mergeGroups.ToArray(), singles);
    }

    /// <summary>
    /// Stacks per-part chapter lists into one timeline: part N's chapters are shifted
    /// by the sum of the preceding parts' durations (seconds). Durations and chapter
    /// times are in ms internally.
    /// </summary>
    public static List<(int Start, int End, string Title)> MergeChapters(
        (List<(int Start, int End, string Title)> Chapters, double DurationSeconds)[] parts)
    {
        var merged = new List<(int Start, int End, string Title)>();
        double offsetSeconds = 0;
        foreach (var (chapters, durationSeconds) in parts)
        {
            var offsetMs = (long)Math.Round(offsetSeconds * 1000.0);
            foreach (var (start, end, title) in chapters)
                merged.Add(((int)(offsetMs + start), (int)(offsetMs + end), title));
            offsetSeconds += durationSeconds;
        }
        return merged;
    }

    /// <summary>
    /// Reads [CHAPTER] blocks from an ffmpeg metadata file (TIMEBASE=1/1000 assumed).
    /// </summary>
    public static List<(int Start, int End, string Title)> ParseFfmetadata(string text)
    {
        var chapters = new List<(int Start, int End, string Title)>();
        int? start = null, end = null;
        string? title = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line == "[CHAPTER]")
            {
                if (start.HasValue && end.HasValue)
                    chapters.Add((start.Value, end.Value, title ?? string.Empty));
                start = end = null;
                title = null;
            }
            else if (line.StartsWith("START=", StringComparison.OrdinalIgnoreCase))
            {
                start = int.TryParse(line.AsSpan(6), out var v) ? v : null;
            }
            else if (line.StartsWith("END=", StringComparison.OrdinalIgnoreCase))
            {
                end = int.TryParse(line.AsSpan(4), out var v) ? v : null;
            }
            else if (line.StartsWith("title=", StringComparison.OrdinalIgnoreCase))
            {
                title = line[6..];
            }
            else if (line.Length == 0 && start.HasValue && end.HasValue)
            {
                chapters.Add((start.Value, end.Value, title ?? string.Empty));
                start = end = null;
                title = null;
            }
        }
        if (start.HasValue && end.HasValue)
            chapters.Add((start.Value, end.Value, title ?? string.Empty));
        return chapters;
    }

    /// <summary>
    /// Builds an ffmpeg metadata file with the merged chapter timeline
    /// (1/1000 timebase - matching the stacker's millisecond domain).
    /// </summary>
    public static string BuildFfmetadataText(IEnumerable<(int Start, int End, string Title)> chapters)
    {
        var sb = new StringBuilder();
        sb.AppendLine(";FFMETADATA1");
        foreach (var (start, end, title) in chapters)
        {
            sb.AppendLine("[CHAPTER]");
            sb.AppendLine("TIMEBASE=1/1000");
            sb.AppendLine($"START={start}");
            sb.AppendLine($"END={end}");
            sb.AppendLine($"title={title}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Decides whether an already-merged final M4B can be skipped: it must exist and
    /// be newer than every part's source file. A missing source is ignored (vanished
    /// files cannot be newer), and a missing or stale final always forces a rebuild.
    /// </summary>
    public static bool ShouldSkipMerge(string finalM4bPath, IEnumerable<string> partSourcePaths)
    {
        if (!File.Exists(finalM4bPath))
            return false;
        var final = File.GetLastWriteTimeUtc(finalM4bPath);
        foreach (var src in partSourcePaths)
        {
            if (!File.Exists(src))
                continue;
            if (File.GetLastWriteTimeUtc(src) > final)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Builds the concat-demuxer file list, escaping single quotes in paths.
    /// </summary>
    public static string BuildConcatList(IEnumerable<string> files)
    {
        var sb = new StringBuilder();
        foreach (var f in files)
            sb.Append("file '").Append(f.Replace("'", "''")).Append("'\n");
        return sb.ToString();
    }
}