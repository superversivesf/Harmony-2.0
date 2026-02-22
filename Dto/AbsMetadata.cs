namespace Harmony.Dto;

using System.Text.Json.Serialization;

public class AbsMetadata
{
    [JsonPropertyName("tags")]
    public List<string>? tags { get; set; }

    [JsonPropertyName("title")]
    public string? title { get; set; }

    [JsonPropertyName("subtitle")]
    public string? subtitle { get; set; }

    [JsonPropertyName("authors")]
    public List<string>? authors { get; set; }

    [JsonPropertyName("narrators")]
    public List<string>? narrators { get; set; }

    [JsonPropertyName("series")]
    public List<string>? series { get; set; }

    [JsonPropertyName("genres")]
    public List<string>? genres { get; set; }

    [JsonPropertyName("publishedYear")]
    public string? publishedYear { get; set; }

    [JsonPropertyName("publishedDate")]
    public string? publishedDate { get; set; }

    [JsonPropertyName("publisher")]
    public string? publisher { get; set; }

    [JsonPropertyName("description")]
    public string? description { get; set; }

    [JsonPropertyName("isbn")]
    public string? isbn { get; set; }

    [JsonPropertyName("asin")]
    public string? asin { get; set; }

    [JsonPropertyName("language")]
    public string? language { get; set; }

    [JsonPropertyName("explicit")]
    public bool @explicit { get; set; }

    [JsonPropertyName("abridged")]
    public bool abridged { get; set; }
}