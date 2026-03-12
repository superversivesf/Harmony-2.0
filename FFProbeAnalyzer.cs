namespace Harmony;

using Xabe.FFmpeg;

internal class FFProbeAnalyzer
{
    private readonly Logger? _logger;

    public FFProbeAnalyzer()
    {
        _logger = null;
    }

    public FFProbeAnalyzer(Logger logger)
    {
        _logger = logger;
    }

    public async Task<bool> AnalyzeFile(string filePath)
    {
        try
        {
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath).ConfigureAwait(false);
            return mediaInfo is not null;
        }
        catch (Exception ex)
        {
            _logger?.WriteLine($"FFProbe analysis failed for '{filePath}': {ex.Message}");
            return false;
        }
    }
}