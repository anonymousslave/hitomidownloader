using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Reactive.Linq;
using Dapper.Contrib.Extensions;

namespace hitomiDownloader.Models
{
    public struct Tagdata
    {
        [JsonProperty(PropertyName ="s")]
        public string Tag { get; set; }
        [JsonProperty(PropertyName ="t")]
        public int Count { get; set; }
    }
    public struct TagdataCollection
    {
        [JsonProperty(PropertyName = "language")]
        public IEnumerable<Tagdata> language { get; set; }
        [JsonProperty(PropertyName = "female")]
        public IEnumerable<Tagdata> female { get; set; }
        [JsonProperty(PropertyName = "series")]
        public IEnumerable<Tagdata> series { get; set; }
        [JsonProperty(PropertyName = "character")]
        public IEnumerable<Tagdata> character { get; set; }
        [JsonProperty(PropertyName = "artist")]
        public IEnumerable<Tagdata> artist { get; set; }
        [JsonProperty(PropertyName = "group")]
        public IEnumerable<Tagdata> group { get; set; }
        [JsonProperty(PropertyName = "tag")]
        public IEnumerable<Tagdata> tag { get; set; }
        [JsonProperty(PropertyName = "male")]
        public IEnumerable<Tagdata> male { get; set; }

        public List<Tag> ToTag(){

            return language.Select(x => $"language:{x.Tag}")
                .Concat(artist.Select(x => $"artists:{x.Tag}"))
                .Concat(group.Select(x => $"groups:{x.Tag}"))
                .Concat(series.Select(x => $"parodies:{x.Tag}"))
                .Concat(character.Select(x => $"characters:{x.Tag}"))
                .Concat(tag.Select(x => $"tags:{x.Tag}"))
                .Concat(male.Select(x => $"male:{x.Tag}"))
                .Concat(female.Select(x => $"female:{x.Tag}"))
                .Concat(new[] { "type:doujinshi","type:manga","type:artistcg","type:gamecg","type:anime"})
                .Select((x, i) => new Tag() { ID = i, Content = x }).ToList();
        }

    }
    [Table("Tag")]
    public class Tag
    {
        public int ID { get; set; }
        public string Content { get; set; }
    }
}
