// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

using Newtonsoft.Json;

public class Chapter
    {
        [JsonProperty("id")]
        public int id { get; set; }

        [JsonProperty("time_base")]
        public string time_base { get; set; }

        [JsonProperty("start")]
        public object start { get; set; }

        [JsonProperty("start_time")]
        public string start_time { get; set; }

        [JsonProperty("end")]
        public object end { get; set; }

        [JsonProperty("end_time")]
        public string end_time { get; set; }

        [JsonProperty("tags")]
        public Tags tags { get; set; }
    }

    public class Format
    {
        [JsonProperty("filename")]
        public string filename { get; set; }

        [JsonProperty("nb_streams")]
        public int nb_streams { get; set; }

        [JsonProperty("nb_programs")]
        public int nb_programs { get; set; }

        [JsonProperty("format_name")]
        public string format_name { get; set; }

        [JsonProperty("format_long_name")]
        public string format_long_name { get; set; }

        [JsonProperty("start_time")]
        public string start_time { get; set; }

        [JsonProperty("duration")]
        public string duration { get; set; }

        [JsonProperty("size")]
        public string size { get; set; }

        [JsonProperty("bit_rate")]
        public string bit_rate { get; set; }

        [JsonProperty("probe_score")]
        public int probe_score { get; set; }

        [JsonProperty("tags")]
        public Tags tags { get; set; }
    }

    public class AaxInfoDto
    {
        [JsonProperty("chapters")]
        public List<Chapter> chapters { get; set; }

        [JsonProperty("format")]
        public Format format { get; set; }
    }

    public class Tags
    {
        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("major_brand")]
        public string major_brand { get; set; }

        [JsonProperty("minor_version")]
        public string minor_version { get; set; }

        [JsonProperty("compatible_brands")]
        public string compatible_brands { get; set; }

        [JsonProperty("creation_time")]
        public DateTime creation_time { get; set; }

        [JsonProperty("comment")]
        public string comment { get; set; }

        [JsonProperty("artist")]
        public string artist { get; set; }

        [JsonProperty("album_artist")]
        public string album_artist { get; set; }

        [JsonProperty("album")]
        public string album { get; set; }

        [JsonProperty("genre")]
        public string genre { get; set; }

        [JsonProperty("copyright")]
        public string copyright { get; set; }

        [JsonProperty("date")]
        public string date { get; set; }
    }

