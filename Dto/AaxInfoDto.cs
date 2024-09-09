using System.Text.Json.Serialization;

namespace Harmony.Dto;

public class AaxInfoDto
{
    [JsonPropertyName("format")]
    public AudioFormatDto Format { get; set; }

    [JsonPropertyName("chapters")]
    public AudioChaptersDto Chapters { get; set; }

    [JsonPropertyName("streams")]
    public AudioStreamDto Streams { get; set; }

    public AaxInfoDto(AudioFormatDto audioFormat, AudioChaptersDto audioChapters, AudioStreamDto audioStreams)
    {
        Format = audioFormat;
        Chapters = audioChapters;
        Streams = audioStreams;
    }
}