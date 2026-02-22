namespace Harmony.Dto;

using System.Text.Json.Serialization;

public class Chapter
{
    [JsonPropertyName("length_ms")]
    public int? length_ms { get; set; }

    [JsonPropertyName("start_offset_ms")]
    public int? start_offset_ms { get; set; }

    [JsonPropertyName("start_offset_sec")]
    public int? start_offset_sec { get; set; }

    [JsonPropertyName("title")]
    public string title { get; set; } = string.Empty;

    [JsonPropertyName("chapters")]
    public List<Chapter>? chapters { get; set; }
}

public class ChapterInfo
{
    [JsonPropertyName("brandIntroDurationMs")]
    public int? brandIntroDurationMs { get; set; }

    [JsonPropertyName("brandOutroDurationMs")]
    public int? brandOutroDurationMs { get; set; }

    [JsonPropertyName("chapters")]
    public List<Chapter>? chapters { get; set; }

    [JsonPropertyName("is_accurate")]
    public bool? is_accurate { get; set; }

    [JsonPropertyName("runtime_length_ms")]
    public int? runtime_length_ms { get; set; }

    [JsonPropertyName("runtime_length_sec")]
    public int? runtime_length_sec { get; set; }
}

public class ContentMetadata
{
    [JsonPropertyName("chapter_info")]
    public ChapterInfo? chapter_info { get; set; }

    [JsonPropertyName("content_reference")]
    public ContentReference? content_reference { get; set; }

    [JsonPropertyName("last_position_heard")]
    public LastPositionHeard? last_position_heard { get; set; }
}

public class ContentReference
{
    [JsonPropertyName("acr")]
    public string? acr { get; set; }

    [JsonPropertyName("asin")]
    public string? asin { get; set; }

    [JsonPropertyName("codec")]
    public string? codec { get; set; }

    [JsonPropertyName("content_format")]
    public string? content_format { get; set; }

    [JsonPropertyName("content_size_in_bytes")]
    public long? content_size_in_bytes { get; set; }

    [JsonPropertyName("file_version")]
    public string? file_version { get; set; }

    [JsonPropertyName("marketplace")]
    public string? marketplace { get; set; }

    [JsonPropertyName("sku")]
    public string? sku { get; set; }

    [JsonPropertyName("tempo")]
    public string? tempo { get; set; }

    [JsonPropertyName("version")]
    public string? version { get; set; }
}

public class LastPositionHeard
{
    [JsonPropertyName("status")]
    public string? status { get; set; }
}

public class AudibleChaptersDto
{
    [JsonPropertyName("content_metadata")]
    public ContentMetadata? content_metadata { get; set; }

    [JsonPropertyName("response_groups")]
    public List<string>? response_groups { get; set; }

    public static List<Chapter> FlattenChapters(List<Chapter>? rootChapters)
    {
        var flattenedChapters = new List<Chapter>();
        FlattenChaptersRecursive(rootChapters, flattenedChapters);
        return flattenedChapters;
    }

    private static void FlattenChaptersRecursive(List<Chapter>? chapters, List<Chapter> flattenedChapters)
    {
        if (chapters == null) return;

        foreach (var chapter in chapters)
        {
            flattenedChapters.Add(chapter);

            if (chapter.chapters != null && chapter.chapters.Count > 0)
            {
                FlattenChaptersRecursive(chapter.chapters, flattenedChapters);
            }
        }
    }
}