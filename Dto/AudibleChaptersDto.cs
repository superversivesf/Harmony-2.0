using Newtonsoft.Json;

namespace Harmony.Dto;


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Chapter
    {
        [JsonProperty("length_ms")]
        public int? length_ms { get; set; }

        [JsonProperty("start_offset_ms")]
        public int? start_offset_ms { get; set; }

        [JsonProperty("start_offset_sec")]
        public int? start_offset_sec { get; set; }

        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("chapters")]
        public List<Chapter> chapters { get; set; }
    }

    public class ChapterInfo
    {
        [JsonProperty("brandIntroDurationMs")]
        public int? brandIntroDurationMs { get; set; }

        [JsonProperty("brandOutroDurationMs")]
        public int? brandOutroDurationMs { get; set; }

        [JsonProperty("chapters")]
        public List<Chapter> chapters { get; set; }

        [JsonProperty("is_accurate")]
        public bool? is_accurate { get; set; }

        [JsonProperty("runtime_length_ms")]
        public int? runtime_length_ms { get; set; }

        [JsonProperty("runtime_length_sec")]
        public int? runtime_length_sec { get; set; }
    }

    public class ContentMetadata
    {
        [JsonProperty("chapter_info")]
        public ChapterInfo chapter_info { get; set; }

        [JsonProperty("content_reference")]
        public ContentReference content_reference { get; set; }

        [JsonProperty("last_position_heard")]
        public LastPositionHeard last_position_heard { get; set; }
    }

    public class ContentReference
    {
        [JsonProperty("acr")]
        public string acr { get; set; }

        [JsonProperty("asin")]
        public string asin { get; set; }

        [JsonProperty("codec")]
        public string codec { get; set; }

        [JsonProperty("content_format")]
        public string content_format { get; set; }

        [JsonProperty("content_size_in_bytes")]
        public int? content_size_in_bytes { get; set; }

        [JsonProperty("file_version")]
        public string file_version { get; set; }

        [JsonProperty("marketplace")]
        public string marketplace { get; set; }

        [JsonProperty("sku")]
        public string sku { get; set; }

        [JsonProperty("tempo")]
        public string tempo { get; set; }

        [JsonProperty("version")]
        public string version { get; set; }
    }

    public class LastPositionHeard
    {
        [JsonProperty("status")]
        public string status { get; set; }
    }

    public class AudibleChaptersDto
    {
        [JsonProperty("content_metadata")]
        public ContentMetadata content_metadata { get; set; }

        [JsonProperty("response_groups")]
        public List<string> response_groups { get; set; }
        
        public static List<Chapter> FlattenChapters(List<Chapter> rootChapters)
        {
            var flattenedChapters = new List<Chapter>();
            FlattenChaptersRecursive(rootChapters, flattenedChapters);
            return flattenedChapters;
        }

        private static void FlattenChaptersRecursive(List<Chapter> chapters, List<Chapter> flattenedChapters)
        {
            if (chapters == null) return;

            foreach (var chapter in chapters)
            {
                flattenedChapters.Add(chapter);

                if (chapter.chapters != null && chapter.chapters.Count > 0)
                {
                    FlattenChaptersRecursive(chapter.chapters, flattenedChapters);
                }
            }
        }

    }

