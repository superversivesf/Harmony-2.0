using System.Text.Json.Serialization;

namespace Harmony.Dto;

public class AudioStreamDto
{
    [JsonPropertyName("streams")]
    public List<StreamDto>? streams { get; set; }
}

public class StreamDto
{
    [JsonPropertyName("index")]
    public int index { get; set; }

    [JsonPropertyName("codec_name")]
    public string? codecName { get; set; }

    [JsonPropertyName("codec_long_name")]
    public string? codecLongName { get; set; }

    [JsonPropertyName("profile")]
    public string? profile { get; set; }

    [JsonPropertyName("codec_type")]
    public string? codecType { get; set; }

    [JsonPropertyName("codec_tag_string")]
    public string? codecTagString { get; set; }

    [JsonPropertyName("codec_tag")]
    public string? codecTag { get; set; }

    [JsonPropertyName("sample_fmt")]
    public string? sampleFormat { get; set; }

    [JsonPropertyName("sample_rate")]
    public string? sampleRate { get; set; }

    [JsonPropertyName("channels")]
    public int channels { get; set; }

    [JsonPropertyName("channel_layout")]
    public string? channelLayout { get; set; }

    [JsonPropertyName("bits_per_sample")]
    public int bitsPerSample { get; set; }

    [JsonPropertyName("r_frame_rate")]
    public string? rFrameRate { get; set; }

    [JsonPropertyName("avg_frame_rate")]
    public string? avgFrameRate { get; set; }

    [JsonPropertyName("time_base")]
    public string? timeBase { get; set; }

    [JsonPropertyName("start_pts")]
    public int startPts { get; set; }

    [JsonPropertyName("start_time")]
    public string? startTime { get; set; }

    [JsonPropertyName("duration_ts")]
    public long durationTs { get; set; }

    [JsonPropertyName("duration")]
    public string? duration { get; set; }

    [JsonPropertyName("bit_rate")]
    public string? bitRate { get; set; }

    [JsonPropertyName("nb_frames")]
    public string? numberOfFrames { get; set; }

    [JsonPropertyName("disposition")]
    public DispositionDto? disposition { get; set; }

    [JsonPropertyName("tags")]
    public StreamTagsDto? tags { get; set; }

    // Optional properties
    [JsonPropertyName("width")]
    public int? width { get; set; }

    [JsonPropertyName("height")]
    public int? height { get; set; }

    [JsonPropertyName("coded_width")]
    public int? codedWidth { get; set; }

    [JsonPropertyName("coded_height")]
    public int? codedHeight { get; set; }

    [JsonPropertyName("closed_captions")]
    public int? closedCaptions { get; set; }

    [JsonPropertyName("has_b_frames")]
    public int? hasBFrames { get; set; }

    [JsonPropertyName("sample_aspect_ratio")]
    public string? sampleAspectRatio { get; set; }

    [JsonPropertyName("display_aspect_ratio")]
    public string? displayAspectRatio { get; set; }

    [JsonPropertyName("pix_fmt")]
    public string? pixelFormat { get; set; }

    [JsonPropertyName("level")]
    public int? level { get; set; }

    [JsonPropertyName("color_range")]
    public string? colorRange { get; set; }

    [JsonPropertyName("color_space")]
    public string? colorSpace { get; set; }

    [JsonPropertyName("chroma_location")]
    public string? chromaLocation { get; set; }

    [JsonPropertyName("refs")]
    public int? refs { get; set; }

    [JsonPropertyName("bits_per_raw_sample")]
    public string? bitsPerRawSample { get; set; }
}

public class DispositionDto
{
    [JsonPropertyName("default")]
    public int @default { get; set; }

    [JsonPropertyName("dub")]
    public int dub { get; set; }

    [JsonPropertyName("original")]
    public int original { get; set; }

    [JsonPropertyName("comment")]
    public int comment { get; set; }

    [JsonPropertyName("lyrics")]
    public int lyrics { get; set; }

    [JsonPropertyName("karaoke")]
    public int karaoke { get; set; }

    [JsonPropertyName("forced")]
    public int forced { get; set; }

    [JsonPropertyName("hearing_impaired")]
    public int hearingImpaired { get; set; }

    [JsonPropertyName("visual_impaired")]
    public int visualImpaired { get; set; }

    [JsonPropertyName("clean_effects")]
    public int cleanEffects { get; set; }

    [JsonPropertyName("attached_pic")]
    public int attachedPic { get; set; }

    [JsonPropertyName("timed_thumbnails")]
    public int timedThumbnails { get; set; }
}

public class StreamTagsDto
{
    [JsonPropertyName("creation_time")]
    public DateTime creationTime { get; set; }

    [JsonPropertyName("language")]
    public string? language { get; set; }

    [JsonPropertyName("handler_name")]
    public string? handlerName { get; set; }

    [JsonPropertyName("vendor_id")]
    public string? vendorId { get; set; }
}