using System.Text.Json.Serialization;

namespace Harmony.Dto.AudioStream;
// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class Disposition
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
        public int hearing_impaired { get; set; }

        [JsonPropertyName("visual_impaired")]
        public int visual_impaired { get; set; }

        [JsonPropertyName("clean_effects")]
        public int clean_effects { get; set; }

        [JsonPropertyName("attached_pic")]
        public int attached_pic { get; set; }

        [JsonPropertyName("timed_thumbnails")]
        public int timed_thumbnails { get; set; }
    }

    public class AudioStream
    {
        [JsonPropertyName("streams")]
        public List<Stream> streams { get; set; } 
    }

    public class Stream
    {
        [JsonPropertyName("index")]
        public int index { get; set; }

        [JsonPropertyName("codec_name")]
        public string codec_name { get; set; }

        [JsonPropertyName("codec_long_name")]
        public string codec_long_name { get; set; }

        [JsonPropertyName("profile")]
        public string profile { get; set; }

        [JsonPropertyName("codec_type")]
        public string codec_type { get; set; }

        [JsonPropertyName("codec_tag_string")]
        public string codec_tag_string { get; set; }

        [JsonPropertyName("codec_tag")]
        public string codec_tag { get; set; }

        [JsonPropertyName("sample_fmt")]
        public string sample_fmt { get; set; }

        [JsonPropertyName("sample_rate")]
        public string sample_rate { get; set; }

        [JsonPropertyName("channels")]
        public int channels { get; set; }

        [JsonPropertyName("channel_layout")]
        public string channel_layout { get; set; }

        [JsonPropertyName("bits_per_sample")]
        public int bits_per_sample { get; set; }

        [JsonPropertyName("r_frame_rate")]
        public string r_frame_rate { get; set; }

        [JsonPropertyName("avg_frame_rate")]
        public string avg_frame_rate { get; set; }

        [JsonPropertyName("time_base")]
        public string time_base { get; set; }

        [JsonPropertyName("start_pts")]
        public int start_pts { get; set; }

        [JsonPropertyName("start_time")]
        public string start_time { get; set; }

        [JsonPropertyName("duration_ts")]
        public long duration_ts { get; set; }

        [JsonPropertyName("duration")]
        public string duration { get; set; }

        [JsonPropertyName("bit_rate")]
        public string bit_rate { get; set; }

        [JsonPropertyName("nb_frames")]
        public string nb_frames { get; set; }

        [JsonPropertyName("disposition")]
        public Disposition disposition { get; set; }

        [JsonPropertyName("tags")]
        public Tags tags { get; set; }

        [JsonPropertyName("width")]
        public int? width { get; set; }

        [JsonPropertyName("height")]
        public int? height { get; set; }

        [JsonPropertyName("coded_width")]
        public int? coded_width { get; set; }

        [JsonPropertyName("coded_height")]
        public int? coded_height { get; set; }

        [JsonPropertyName("closed_captions")]
        public int? closed_captions { get; set; }

        [JsonPropertyName("has_b_frames")]
        public int? has_b_frames { get; set; }

        [JsonPropertyName("sample_aspect_ratio")]
        public string sample_aspect_ratio { get; set; }

        [JsonPropertyName("display_aspect_ratio")]
        public string display_aspect_ratio { get; set; }

        [JsonPropertyName("pix_fmt")]
        public string pix_fmt { get; set; }

        [JsonPropertyName("level")]
        public int? level { get; set; }

        [JsonPropertyName("color_range")]
        public string color_range { get; set; }

        [JsonPropertyName("color_space")]
        public string color_space { get; set; }

        [JsonPropertyName("chroma_location")]
        public string chroma_location { get; set; }

        [JsonPropertyName("refs")]
        public int? refs { get; set; }

        [JsonPropertyName("bits_per_raw_sample")]
        public string bits_per_raw_sample { get; set; }
    }

    public class Tags
    {
        [JsonPropertyName("creation_time")]
        public DateTime creation_time { get; set; }

        [JsonPropertyName("language")]
        public string language { get; set; }

        [JsonPropertyName("handler_name")]
        public string handler_name { get; set; }

        [JsonPropertyName("vendor_id")]
        public string vendor_id { get; set; }
    }

