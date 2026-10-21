namespace Harmony.Dto;

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

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

    [TypeConverter(typeof(LenientNullableIntConverter))]
    public int? runtime_length_min { get; set; }

    [TypeConverter(typeof(LenientNullableBoolConverter))]
    public bool? is_finished { get; set; }

    [TypeConverter(typeof(LenientNullableDoubleConverter))]
    public double? percent_complete { get; set; }

    [TypeConverter(typeof(LenientNullableDoubleConverter))]
    public double? rating { get; set; }

    [TypeConverter(typeof(LenientNullableIntConverter))]
    public int? num_ratings { get; set; }

    [TypeConverter(typeof(LenientNullableDateConverter))]
    public DateTime? date_added { get; set; }

    [TypeConverter(typeof(LenientNullableDateConverter))]
    public DateTime? release_date { get; set; }

    public string? cover_url { get; set; }

    [TypeConverter(typeof(LenientNullableDateConverter))]
    public DateTime? purchase_date { get; set; }

    /// <summary>
    /// The authors as a trimmed list; audible-cli joins names with ", ".
    /// </summary>
    public string[] GetAuthorsList() => SplitTrim(authors);

    /// <summary>
    /// The narrators as a trimmed list; audible-cli joins names with ", ".
    /// </summary>
    public string[] GetNarratorsList() => SplitTrim(narrators);

    /// <summary>
    /// The genres as a trimmed list; audible-cli joins category names with ", ".
    /// </summary>
    public string[] GetGenresList() => SplitTrim(genres);

    private static string[] SplitTrim(string? joined)
    {
        return joined?.Split(',')
            .Select(g => g.Trim())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .ToArray() ?? Array.Empty<string>();
    }
}

internal static class LenientCellHelpers
{
    /// <summary>
    /// True for cells audible-cli exports as placeholders: blank (omitted optional
    /// fields) or "-" (missing ratings and cover URLs).
    /// </summary>
    public static bool IsBlankOrPlaceholder(string? text)
    {
        return string.IsNullOrWhiteSpace(text) || text == "-";
    }
}

/// <summary>
/// Lenient CsvHelper converter for nullable numeric cells; audible-cli exports "-" or
/// blank for missing values and neither should abort the library parse.
/// </summary>
public class LenientNullableDoubleConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (LenientCellHelpers.IsBlankOrPlaceholder(text))
            return null;
        return double.TryParse(text, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}

/// <summary>
/// Lenient CsvHelper converter for nullable integer cells; tolerates "-" and blanks.
/// </summary>
public class LenientNullableIntConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (LenientCellHelpers.IsBlankOrPlaceholder(text))
            return null;
        return int.TryParse(text, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}

/// <summary>
/// Lenient CsvHelper converter for nullable boolean cells; tolerates "-" and blanks.
/// </summary>
public class LenientNullableBoolConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (LenientCellHelpers.IsBlankOrPlaceholder(text))
            return null;
        return bool.TryParse(text, out var value) ? value : null;
    }
}

/// <summary>
/// Lenient CsvHelper converter for nullable date cells; tolerates "-" and blanks.
/// </summary>
public class LenientNullableDateConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (LenientCellHelpers.IsBlankOrPlaceholder(text))
            return null;
        return DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var value)
            ? value
            : null;
    }
}