namespace Harmony.Dto;

/// <summary>
/// Represents an entry from the Audible library TSV export.
/// Property names match TSV column headers for CsvHelper mapping.
/// </summary>
public class AudibleLibraryDto
{
    public string? asin { get; set; }
    public string? title { get; set; }
    public string? subtitle { get; set; }
    public string? extended_product_description { get; set; }
    public string? authors { get; set; }
    public string? narrators { get; set; }
    public string? series_title { get; set; }
    public string? series_sequence { get; set; }
    public string? genres { get; set; }
    public int runtime_length_min { get; set; }
    public bool is_finished { get; set; }
    public double? percent_complete { get; set; }
    public double? rating { get; set; }
    public int? num_ratings { get; set; }
    public DateTime date_added { get; set; }
    public DateTime release_date { get; set; }
    public string? cover_url { get; set; }
    public DateTime purchase_date { get; set; }
}