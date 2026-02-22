// This file uses Newtonsoft.Json attributes ([JsonProperty]) instead of System.Text.Json
// ([JsonPropertyName]) due to the consumer AAXCtoM4BConvertor, which uses JsonConvert.DeserializeObject
// for deserialization. Switching to System.Text.Json would require updating the consumer to use
// JsonSerializer.Deserialize. Retaining Newtonsoft.Json ensures compatibility without breaking changes.
// See: AAXCtoM4BConvertor.cs (JsonConvert.DeserializeObject<AudibleVoucherDto>)

namespace Harmony.Dto;

using Newtonsoft.Json;

public class ContentLicense
{
    [JsonProperty("access_expiry_date")]
    public DateTime? access_expiry_date { get; set; }

    [JsonProperty("acr")]
    public string? acr { get; set; }

    [JsonProperty("allowed_users")]
    public List<string>? allowed_users { get; set; }

    [JsonProperty("asin")]
    public string? asin { get; set; }

    [JsonProperty("content_metadata")]
    public ContentMetadata? content_metadata { get; set; }

    [JsonProperty("drm_type")]
    public string? drm_type { get; set; }

    [JsonProperty("granted_right")]
    public string? granted_right { get; set; }

    [JsonProperty("license_id")]
    public string? license_id { get; set; }

    [JsonProperty("license_response")]
    public LicenseResponse? license_response { get; set; }

    [JsonProperty("license_response_type")]
    public string? license_response_type { get; set; }

    [JsonProperty("message")]
    public string? message { get; set; }

    [JsonProperty("playback_info")]
    public PlaybackInfo? playback_info { get; set; }

    [JsonProperty("preview")]
    public bool? preview { get; set; }

    [JsonProperty("refresh_date")]
    public DateTime? refresh_date { get; set; }

    [JsonProperty("removal_date")]
    public DateTime? removal_date { get; set; }

    [JsonProperty("request_id")]
    public string? request_id { get; set; }

    [JsonProperty("requires_ad_supported_playback")]
    public bool? requires_ad_supported_playback { get; set; }

    [JsonProperty("status_code")]
    public string? status_code { get; set; }

    [JsonProperty("voucher_id")]
    public string? voucher_id { get; set; }
}

public class ContentUrl
{
    [JsonProperty("offline_url")]
    public string? offline_url { get; set; }
}

public class LicenseResponse
{
    [JsonProperty("key")]
    public string? key { get; set; }

    [JsonProperty("iv")]
    public string? iv { get; set; }

    [JsonProperty("refreshDate")]
    public DateTime? refreshDate { get; set; }

    [JsonProperty("removalOnExpirationDate")]
    public DateTime? removalOnExpirationDate { get; set; }

    [JsonProperty("rules")]
    public List<Rule>? rules { get; set; }
}

public class Parameter
{
    [JsonProperty("expireDate")]
    public DateTime? expireDate { get; set; }

    [JsonProperty("type")]
    public string? type { get; set; }

    [JsonProperty("directedIds")]
    public List<string>? directedIds { get; set; }
}

public class PlaybackInfo
{
    [JsonProperty("last_position_heard")]
    public LastPositionHeard? last_position_heard { get; set; }
}

public class AudibleVoucherDto
{
    [JsonProperty("content_license")]
    public ContentLicense? content_license { get; set; }

    [JsonProperty("response_groups")]
    public List<string>? response_groups { get; set; }
}

public class Rule
{
    [JsonProperty("parameters")]
    public List<Parameter>? parameters { get; set; }

    [JsonProperty("name")]
    public string? name { get; set; }
}