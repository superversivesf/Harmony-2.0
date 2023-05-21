namespace Harmony.Dto;

internal class AaxInfoDto
{
    public AaxInfoDto(AudioFormat.AudioFormat audioFormat, AudioChapters.AudioChapters audioChaptersDto, AudioStream.AudioStream audioStreams)
    {
        format = audioFormat;
        chapters = audioChaptersDto;
        streams = audioStreams;
    }

    public AudioFormat.AudioFormat format { get; set; }
    public AudioChapters.AudioChapters chapters { get; set; }
    public AudioStream.AudioStream streams { get; set; }
}
