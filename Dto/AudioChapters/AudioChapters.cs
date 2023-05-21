using System.Text.Json.Serialization;

namespace Harmony.Dto.AudioChapters;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class Chapter
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("time_base")]
    public string time_base { get; set; }

    [JsonPropertyName("start")]
    public int start { get; set; }

    [JsonPropertyName("start_time")]
    public string start_time { get; set; }

    [JsonPropertyName("end")]
    public int end { get; set; }

    [JsonPropertyName("end_time")]
    public string end_time { get; set; }

    [JsonPropertyName("tags")]
    public Tags tags { get; set; }
}

public class AudioChapters
{
    [JsonPropertyName("chapters")]
    public List<Chapter> chapters { get; } = new List<Chapter>();
}

public class Tags
{
    [JsonPropertyName("title")]
    public string title { get; set; }
}

