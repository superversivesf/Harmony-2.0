namespace Harmony;

using Xabe.FFmpeg;

public class FFProbeAnalyzer
{
    public async Task<bool> AnalyzeFile(string filePath)
    {
        try
        {
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath).ConfigureAwait(false);
            return mediaInfo != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}