using Dapper.Contrib.Extensions;
using hitomiDownloader.Core;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace hitomiDownloader.Models
{
    public struct JsonMetadata
    {
        [JsonProperty(PropertyName = "a")]
        public string[] Artists { get; set; }
        [JsonProperty(PropertyName = "g")]
        public string[] Groups { get; set; }
        [JsonProperty(PropertyName = "p")]
        public string[] Parodies { get; set; }
        [JsonProperty(PropertyName = "t")]
        public string[] Tags { get; set; }
        [JsonProperty(PropertyName = "c")]
        public string[] Characters { get; set; }
        [JsonProperty(PropertyName = "l")]
        public string Language { get; set; }
        [JsonProperty(PropertyName = "n")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        public string MetadataTag { get
            {
              return string.Join(",",
              new[] { $"language:{Language ?? ""}", $"type:{Type}" }
              .Concat(Artists?.Select(x => $"artists:{x}") ?? Enumerable.Empty<string>())
              .Concat(Groups?.Select(x => $"groups:{x}") ?? Enumerable.Empty<string>())
              .Concat(Parodies?.Select(x => $"parodies:{x}") ?? Enumerable.Empty<string>())
              .Concat(Characters?.Select(x => $"characters:{x}") ?? Enumerable.Empty<string>())
              .Concat(Tags?.Select(x =>
              {
                  if (x.ContainsString("male:") || x.ContainsString("female:")) return $"{x}";
                  else return $"tags:{x}";
              }) ?? Enumerable.Empty<string>()));
            }
        }
        
    }
    [Table("Metadata")]
    public class Metadata
    {
        [Key]
        public int ID { get; set; }
        public int MetadataID { get; set; }
        public string Artist { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public string Type { get; set; }
        public string Tags { get; set; }
    }
    public class MetadataModel : ReactiveObject,IDisposable
    {
        public Metadata Metadata { get; set; }
        public int MetadataID => Metadata.MetadataID;
        public int Index { get; set; }
        public string Title => $"{Metadata.Name}({Metadata.MetadataID})";
        public string Language => Metadata.Language ?? "N/A";
        public string Type => Metadata.Type;
        public IEnumerable<string> MetadataTag { get; set; }
        public string Artist => string.IsNullOrWhiteSpace(Metadata.Artist) ? "N/A" : Metadata.Artist;
        public string Parodies => Metadata.Tags.Split(',').Where(x => x.StartsWith("parodies:")).Select(x=>x.Replace("parodies:","")).FirstOrDefault() ?? "N/A";
        public string Page => $"{imageManager.Page}p";

        public ReactiveCommand Download { get; }
        public ReactiveCommand GetURL { get; }
        public ReactiveCommand OpenImageViewer { get; }
        public ReactiveCommand TagClick { get; set; }
        internal readonly ImageManager imageManager;
        public BitmapImage Thumbnail => imageManager.Thumbnail;
        public MetadataModel(Metadata metadata,int index)
        {
            Index = index;
            Metadata = metadata;
            MetadataTag = Metadata.Tags.Split(',');
            imageManager = new ImageManager(this);
            OpenImageViewer = ReactiveCommand.Create(()=>
            {
                imageManager.CurrentIndex = 0;
                imageManager.Preload(0,Common.Setting.preload_number);
                new GalleryViewer(imageManager).Show();
            });
            GetURL= ReactiveCommand.Create(() =>
            {
                Clipboard.SetText($"https://hitomi.la/reader/{metadata.MetadataID}.html");
            });
            Download = ReactiveCommand.Create(()=>DownloadManager.Instance.Enqueue(imageManager));
        }
        public void Refresh()
        {
            this.RaisePropertyChanged(nameof(Page));
            this.RaisePropertyChanged(nameof(Thumbnail));
        }
        public void Dispose()
        {
            imageManager.Release();
        }
    }
}
