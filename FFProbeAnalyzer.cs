namespace Harmony;

using System;
using System.Threading.Tasks;
using Xabe.FFmpeg;

public class FFprobeAnalyzer
{
    public async Task<bool> AnalyzeFile(string filePath)
    {
        try
        {
            
            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath);

            // Check if the file can be read
            if (mediaInfo == null)
            {
                return false;
            }

            var newProbe = Probe.New();
            var result= await newProbe.Start('"' + filePath + '"');
            
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}