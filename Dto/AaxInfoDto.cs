namespace Harmony.Dto;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an FFprobe chapter from AAX file analysis.
/// </summary>
public class AaxChapter
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("time_base")]
    public string time_base { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public object? start { get; set; }

    [JsonPropertyName("start_time")]
    public string start_time { get; set; } = string.Empty;

    [JsonPropertyName("end")]
    public object? end { get; set; }

    [JsonPropertyName("end_time")]
    public string end_time { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public AaxTags? tags { get; set; }
}

/// <summary>
/// Represents FFprobe format metadata from AAX file analysis.
/// </summary>
public class AaxFormat
{
    [JsonPropertyName("filename")]
    public string filename { get; set; } = string.Empty;

    [JsonPropertyName("nb_streams")]
    public int nb_streams { get; set; }

    [JsonPropertyName("nb_programs")]
    public int nb_programs { get; set; }

    [JsonPropertyName("format_name")]
    public string format_name { get; set; } = string.Empty;

    [JsonPropertyName("format_long_name")]
    public string format_long_name { get; set; } = string.Empty;

    [JsonPropertyName("start_time")]
    public string start_time { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string? duration { get; set; }

    [JsonPropertyName("size")]
    public string? size { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? bit_rate { get; set; }

    [JsonPropertyName("probe_score")]
    public int probe_score { get; set; }

    [JsonPropertyName("tags")]
    public AaxTags? tags { get; set; }
}

/// <summary>
/// Root DTO for FFprobe JSON output when analyzing AAX files.
/// </summary>
public class AaxInfoDto
{
    [JsonPropertyName("chapters")]
    public List<AaxChapter>? chapters { get; set; }

    [JsonPropertyName("format")]
    public AaxFormat? format { get; set; }
}

/// <summary>
/// Metadata tags from FFprobe analysis.
/// </summary>
public class AaxTags
{
    [JsonPropertyName("title")]
    public string? title { get; set; }

    [JsonPropertyName("major_brand")]
    public string? major_brand { get; set; }

    [JsonPropertyName("minor_version")]
    public string? minor_version { get; set; }

    [JsonPropertyName("compatible_brands")]
    public string? compatible_brands { get; set; }

    [JsonPropertyName("creation_time")]
    public DateTime? creation_time { get; set; }

    [JsonPropertyName("comment")]
    public string? comment { get; set; }

    [JsonPropertyName("artist")]
    public string? artist { get; set; }

    [JsonPropertyName("album_artist")]
    public string? album_artist { get; set; }

    [JsonPropertyName("album")]
    public string? album { get; set; }

    [JsonPropertyName("genre")]
    public string? genre { get; set; }

    [JsonPropertyName("copyright")]
    public string? copyright { get; set; }

    [JsonPropertyName("date")]
    public string? date { get; set; }
}