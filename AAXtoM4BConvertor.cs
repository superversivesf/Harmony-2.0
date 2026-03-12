using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Harmony.Dto;
using Spectre.Console;
using File = System.IO.File;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Harmony;

/// <summary>
/// Converts AAX audiobook files to M4B format using activation bytes authentication.
/// </summary>
internal class AaxToM4BConvertor : AudiobookConverterBase
{
    /// <summary>
    /// Maximum number of authors to display individually before showing "Various".
    /// </summary>
    public new const int MaxAuthorCountForIndividualDisplay = AudiobookConverterBase.MaxAuthorCountForIndividualDisplay;

    private readonly string _activationBytes;

    public AaxToM4BConvertor(string activationBytes, int bitrate, bool quietMode, string inputFolder,
        string outputFolder, bool clobber, List<AudibleLibraryDto>? library = null, ProgressContextManager? progressManager = null)
        : base(bitrate, quietMode, inputFolder, outputFolder, clobber, library, progressManager)
    {
        _activationBytes = activationBytes;
    }

    /// <summary>
    /// Gets the file extension pattern for AAX files.
    /// </summary>
    protected override string FileExtensionPattern => "*.aax";

    /// <summary>
    /// Gets the authentication parameters for AAX files (activation_bytes).
    /// </summary>
    protected override IEnumerable<string> GetAuthenticationParameters()
    {
        return new[] { "-activation_bytes", _activationBytes };
    }

    /// <summary>
    /// Gets file info by probing the AAX file with activation bytes.
    /// </summary>
    protected override AaxInfoDto? GetFileInfo(string filePath)
    {
        var logger = new Logger(true, false);
        logger.Write("Probing AAX file... ");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.StartInfo.ArgumentList.Add("-v");
        process.StartInfo.ArgumentList.Add("quiet");
        process.StartInfo.ArgumentList.Add("-print_format");
        process.StartInfo.ArgumentList.Add("json");
        process.StartInfo.ArgumentList.Add("-show_format");
        process.StartInfo.ArgumentList.Add("-show_chapters");
        process.StartInfo.ArgumentList.Add("-activation_bytes");
        process.StartInfo.ArgumentList.Add(_activationBytes);
        process.StartInfo.ArgumentList.Add(filePath);

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        logger.WriteLine("Done");

        return JsonSerializer.Deserialize<AaxInfoDto>(output, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
