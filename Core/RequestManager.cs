using hitomiDownloader.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace hitomiDownloader.Core
{
    public class RequestManager
    {
        private static readonly Lazy<RequestManager> _instance = new Lazy<RequestManager>(() => new RequestManager());
        private static HttpClient client;
        private RequestManager()
        {
            client = new HttpClient();
        }
        public async Task<string> DownloadAsString(string url)
        {
            return await client.GetStringAsync(url);
        }
        public async Task<T> DownloadAs<T>(string url,Func<string,string> transform = null)
        {
            var content = await client.GetStringAsync(url);
            if(transform != null)
            {
               content = transform.Invoke(content);
            }
            return JsonConvert.DeserializeObject<T>(content);
        }
        public async Task<Stream> Stream(string url)
        {
            return await client.GetStreamAsync(url);
        }
        public async Task<byte[]> DownloadAsByte(string url)
        {
            return await client.GetByteArrayAsync(url);
        }
        public BitmapImage CreateBitmap(byte[] bytes, bool freezing = true)
        {
            using (var stream = new WrappingStream(new MemoryStream(bytes)))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                if (freezing && bitmap.CanFreeze)
                { bitmap.Freeze(); }
                return bitmap;
            }
        }
        public BitmapImage CreateBitmap(string url,bool freezing = true)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            if (freezing && bitmap.CanFreeze)
            { bitmap.Freeze(); }
            return bitmap;
        }
        public async Task<BitmapImage> DownloadImageAsync(string url,
    Dispatcher dispatcherForBitmapCreation)
        {
            var bytes = await DownloadAsByte(url);
            return await dispatcherForBitmapCreation.InvokeAsync(() => CreateBitmap(bytes));
        }

        public static RequestManager Instance => _instance.Value;
    }
}
