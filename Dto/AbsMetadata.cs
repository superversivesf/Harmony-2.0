namespace Harmony.Dto;

public class AbsMetadata
{
    public List<string> tags { get; set; }
    public string? title { get; set; }
    public string? subtitle { get; set; }
    public List<string>? authors { get; set; }
    public List<string>? narrators { get; set; }
    public List<string> series { get; set; }
    public List<string>? genres { get; set; }
    public string? publishedYear { get; set; }
    public string? publishedDate { get; set; }
    public string publisher { get; set; }
    public string? description { get; set; }
    public string isbn { get; set; }
    public string? asin { get; set; }
    public string language { get; set; }
    public bool @explicit { get; set; }
    public bool abridged { get; set; }
}

