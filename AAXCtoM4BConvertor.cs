using System.Diagnostics;
using System.Text.Json;
using Newtonsoft.Json;
using Harmony.Dto;
using File = System.IO.File;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Harmony;

/// <summary>
/// Converts AAXC audiobook files to M4B format using key/IV authentication from voucher files.
/// </summary>
internal class AaxcToM4BConvertor : AudiobookConverterBase
{
    /// <summary>
    /// Maximum number of authors to display before showing "Various" instead.
    /// </summary>
    internal const int MaxAuthorsBeforeVarious = MaxAuthorCountForIndividualDisplay;

    /// <summary>
    /// Default AAC bitrate used for encoding when copying is not possible.
    /// </summary>
    internal new const string DefaultAacBitrate = AudiobookConverterBase.DefaultAacBitrate;

    private string? _iv;
    private string? _key;

    public AaxcToM4BConvertor(int bitrate, bool quietMode, string inputFolder,
        string outputFolder, bool clobber, List<AudibleLibraryDto>? library = null,
        ProgressContextManager? progressManager = null)
        : base(bitrate, quietMode, inputFolder, outputFolder, clobber, library, progressManager)
    {
    }

    /// <summary>
    /// Gets the file extension pattern for AAXC files.
    /// </summary>
    protected override string FileExtensionPattern => "*.aaxc";

    /// <summary>
    /// Gets the authentication parameters for AAXC files (audible_key and audible_iv).
    /// Must be called after LoadVoucher has set _key and _iv.
    /// </summary>
    protected override IEnumerable<string> GetAuthenticationParameters()
    {
        return new[] { "-audible_key", _key!, "-audible_iv", _iv! };
    }

    /// <summary>
    /// Loads the voucher file for the given AAXC file to extract key and IV.
    /// </summary>
    private bool LoadVoucher(string filePath)
    {
        var voucherFile = Path.ChangeExtension(filePath, "voucher");
        
        if (!File.Exists(voucherFile))
        {
            return false;
        }

        var voucher = JsonConvert.DeserializeObject<AudibleVoucherDto>(File.ReadAllText(voucherFile));

        if (voucher?.content_license?.license_response is null)
        {
            return false;
        }

        _iv = voucher.content_license.license_response.iv;
        _key = voucher.content_license.license_response.key;

        return true;
    }

    /// <summary>
    /// Gets file info by probing the AAXC file with key/IV from voucher.
    /// </summary>
    protected override AaxInfoDto? GetFileInfo(string filePath)
    {
        // Load voucher first to get key/iv
        if (!LoadVoucher(filePath))
        {
            return null;
        }

        var logger = new Logger(true, false);
        logger.Write("Probing AAXC file... ");

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
        process.StartInfo.ArgumentList.Add("-audible_key");
        process.StartInfo.ArgumentList.Add(_key!);
        process.StartInfo.ArgumentList.Add("-audible_iv");
        process.StartInfo.ArgumentList.Add(_iv!);
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

    /// <summary>
    /// Executes the conversion process for all AAXC files in the input folder.
    /// Overrides base to handle voucher loading per file.
    /// </summary>
    internal new async Task ExecuteAsync()
    {
        // Need to check if this is the right pattern - the base ExecuteAsync
        // will call GetFileInfo which loads the voucher
        await base.ExecuteAsync();
    }
}
