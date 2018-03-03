using System;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using hitomiDownloader.Core;
using Dapper.Contrib.Extensions;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ReactiveUI;
using System.Collections;
using System.Windows;
using System.Net;
using System.Reflection;
using System.Net.Http;
using System.Diagnostics;

namespace hitomiDownloader.Models
{
    public struct JsonImageData
    {
        [JsonProperty(PropertyName = "width")]
        public double Width { get; set; }
        [JsonProperty(PropertyName = "height")]
        public double Height { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
    [Table("MetadataImage")]
    public class MetadataImage
    {
        [Key]
        public int ID { get; set; }
        public int ImageID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int MetadataID { get; set; }

        [Write(false)]
        public BitmapImage Image { get; set; }
    }
    public class ImageManager : ReactiveObject {
        public ReactiveList<MetadataImage> Images { get; set; }
        public ReactiveCommand PrevCommand { get; private set; }
        public ReactiveCommand NextCommand { get; private set; }
        private int index;
        private bool isFit;
        public Stretch Stretch { get; set; } = Stretch.Uniform;
        public bool IsFit {
            get { return isFit; }
            set {
                this.RaiseAndSetIfChanged(ref isFit, value);
                if (value)
                {
                    this.Stretch = Stretch.Uniform;
                }
                else
                {
                    this.Stretch = Stretch.UniformToFill;
                }
                this.RaisePropertyChanged(nameof(Stretch));
            }
        }
        public int CurrentIndex {
            get { return index; }
            set {
                this.RaiseAndSetIfChanged(ref index, value);
                if(CurrentIndex % 2 == 0)
                {
                    Preload(CurrentIndex);
                }
            }
        }
        public BitmapImage Thumbnail { get; set; }
        public string Title { get; set; }
        public int ID { get; set; }
        public int Page => Images?.Count ?? 0;
        private Action Refresh;
        public ImageManager(MetadataModel metadata)
        {
            ID = metadata.MetadataID;
            Refresh = metadata.Refresh;
            Title = metadata.Title;
            Initialize();
        }
        internal async void Initialize()
        {
            var o = await DatabaseManager.Instance.GetMetadataImage(ID);
            if (!o.Any())
            {
                await DatabaseManager.Instance.UpdateMetadataImage(ID);
                o = await DatabaseManager.Instance.GetMetadataImage(ID);
            }

            Images = new ReactiveList<MetadataImage>(o);
            if(Images.Any())
                Thumbnail = RequestManager.Instance.CreateBitmap($"https://tn.hitomi.la/smalltn/{ID}/{Images[0].Name}.jpg");
            Refresh();
        }
        public void Preload(int Current,int count = 5)
        {
            Preload(Images.Skip(Current).Take(count));
        }
        private void Preload(IEnumerable<MetadataImage> images)
        {
            foreach (var image in Images)
            {
                try
                {
                    if (image.Image == null) image.Image = RequestManager.Instance.CreateBitmap(image.Url);
                }
                catch (HttpRequestException e)
                {
                    Trace.WriteLine(e.Message);
                }
                catch (TargetInvocationException) { }
            }
        }
        internal void Release()
        {
            if (Images == null) return;
            foreach(var image in Images)
            {
                image.Image = null;
            }
            GC.Collect();
        }
    }
}
