using System.Text.Json.Serialization;

namespace Harmony.Dto;

public class AudioChaptersDto
{
    [JsonPropertyName("chapters")]
    public List<ChapterDto>? chapters { get; set; }
}

public class ChapterDto
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("time_base")]
    public string? timeBase { get; set; }

    [JsonPropertyName("start")]
    public int start { get; set; }

    [JsonPropertyName("start_time")]
    public string? startTime { get; set; }

    [JsonPropertyName("end")]
    public int end { get; set; }

    [JsonPropertyName("end_time")]
    public string? endTime { get; set; }

    [JsonPropertyName("tags")]
    public ChapterTagsDto? tags { get; set; }
}

public class ChapterTagsDto
{
    [JsonPropertyName("title")]
    public string? title { get; set; }
}