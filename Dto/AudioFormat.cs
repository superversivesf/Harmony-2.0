using System.Text.Json.Serialization;

namespace Harmony.Dto;

public class AudioFormatDto
{
    [JsonPropertyName("format")]
    public FormatDto? format { get; set; }
}

public class FormatDto
{
    [JsonPropertyName("filename")]
    public string? filename { get; set; }

    [JsonPropertyName("nb_streams")]
    public int numberOfStreams { get; set; }

    [JsonPropertyName("nb_programs")]
    public int numberOfPrograms { get; set; }

    [JsonPropertyName("format_name")]
    public string? formatName { get; set; }

    [JsonPropertyName("format_long_name")]
    public string? formatLongName { get; set; }

    [JsonPropertyName("start_time")]
    public string? startTime { get; set; }

    [JsonPropertyName("duration")]
    public string? duration { get; set; }

    [JsonPropertyName("size")]
    public string? size { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? bitRate { get; set; }

    [JsonPropertyName("probe_score")]
    public int probeScore { get; set; }

    [JsonPropertyName("tags")]
    public FormatTagsDto? tags { get; set; }
}

public class FormatTagsDto
{
    [JsonPropertyName("major_brand")]
    public string? majorBrand { get; set; }

    [JsonPropertyName("minor_version")]
    public string? minorVersion { get; set; }

    [JsonPropertyName("compatible_brands")]
    public string? compatibleBrands { get; set; }

    [JsonPropertyName("creation_time")]
    public DateTime creationTime { get; set; }

    [JsonPropertyName("comment")]
    public string? comment { get; set; }

    [JsonPropertyName("title")]
    public string? title { get; set; }

    [JsonPropertyName("artist")]
    public string? artist { get; set; }

    [JsonPropertyName("album_artist")]
    public string? albumArtist { get; set; }

    [JsonPropertyName("album")]
    public string? album { get; set; }

    [JsonPropertyName("genre")]
    public string? genre { get; set; }

    [JsonPropertyName("copyright")]
    public string? copyright { get; set; }

    [JsonPropertyName("date")]
    public string? date { get; set; }
}