using Newtonsoft.Json;

namespace hitomiDownloader.Models
{
    public class HitomiSetting
    {
        [JsonProperty]
        public Query[] Queries { get; set; } = { };
        [JsonProperty]
        public string DownloadPath { get; set; }
        // https://hitomi.la/searchlib.js
        [JsonProperty]
        public int number_of_gallery_jsons { get; set; }
        [JsonProperty]
        public int max_number_of_results { get; set; }
        [JsonProperty]
        public int preload_number { get; set; }
        [JsonProperty]
        public bool isCompress { get; set; }
    }
}
