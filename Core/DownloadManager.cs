using hitomiDownloader.Models;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace hitomiDownloader.Core
{
    public class DownloadResult
    {
        public object Source { get; set; }
        public WrappingStream Data { get; set; }
    }
    public class DownloadRequest : IObservable<DownloadResult>
    {
        WebClient client = new WebClient();
        Subject<DownloadResult> subject = new Subject<DownloadResult>();
        public MetadataImage Source { get; set; }
        public DownloadRequest()
        {
            client = new WebClient();
            client.DownloadDataCompleted += Client_DownloadDataCompleted;
        }
        public void Start()
        {
            client.DownloadDataAsync(new Uri(Source.Url), Source);
        }
        public void Cancel()
        {
            client.CancelAsync();
        }
        private void Client_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            try
            {
                var Data = e.Result;
                subject.OnNext(new DownloadResult()
                {
                    Source = e.UserState,
                    Data = new WrappingStream(new MemoryStream(Data))
                });
            }
            catch (WebException) { Cancel(); }
            catch (TargetInvocationException) { }
            finally
            {
                client.DownloadDataCompleted -= Client_DownloadDataCompleted;
                client.Dispose();
            }
        }
        public IDisposable Subscribe(IObserver<DownloadResult> observer)
        {
            return subject.Subscribe(observer);
        }
    }
    public class DownloadItem : ReactiveObject ,IDisposable
    {
        public bool IsCompleted { get; set; }
        public ImageManager ImageManager { get; set; }
        public ReactiveCommand CancelCommand { get; set; }
        public ConcurrentQueue<DownloadRequest> Requests { get; set; }
        public ConcurrentQueue<DownloadResult> Completed { get; set; }
        public string Complection => $"({Completed.Count} / {Requests.Count})";
        public double Progress => ((double)Completed.Count / Requests.Count) * 100;
        public event EventHandler OnCompleted;
        public DownloadItem(ImageManager imageManager)
        {
            this.ImageManager = imageManager;
            this.Requests = new ConcurrentQueue<DownloadRequest>();
            this.Completed = new ConcurrentQueue<DownloadResult>();
            this.CancelCommand = ReactiveCommand.Create(Cancel);
            NotificationManager.NotifyInformation($"{imageManager.Title} 다운로드를 시작합니다.");
            ImageManager.Images.ForEachAsync(Environment.ProcessorCount, async item =>
            {
                await Task.Run(() =>
                {
                    var obj = new DownloadRequest()
                    {
                        Source = item
                    };
                    obj.Start();
                    Requests.Enqueue(obj);
                    obj.Subscribe(result => {
                        Completed.Enqueue(result);
                        this.RaisePropertyChanged(nameof(Complection));
                        this.RaisePropertyChanged(nameof(Progress));
                        if (Progress >= 100)
                        {
                            IsCompleted = true;
                            OnCompleted?.Invoke(this, null);
                        }
                    });
                });
            });
        }
        public void Cancel()
        {
            foreach (var item in Requests)
            {
                item.Cancel();
            }
            DownloadResult result = null;
            while(Completed.TryDequeue(out result))
            {
                result.Data.Dispose();
            }
            IsCompleted = false;
            OnCompleted?.Invoke(this, null);
        }
        public void Dispose()
        {
            Cancel();
        }
    }
    public class DownloadManager
    {
        private static readonly Lazy<DownloadManager> _instance = new Lazy<DownloadManager>(() => new DownloadManager());
        public ObservableCollection<DownloadItem> DownloadQueue { get; set; }
        private DownloadManager()
        {
            DownloadQueue = new ObservableCollection<DownloadItem>();
        }
        public void Enqueue(ImageManager imageManager)
        {
            var item = new DownloadItem(imageManager);
            item.OnCompleted += Item_OnCompleted;
            DownloadQueue.Add(item);
        }

        private async void Item_OnCompleted(object sender, EventArgs e)
        {
            var item = sender as DownloadItem;

            if (!item.IsCompleted)
            {
                NotificationManager.NotifySuccess($"{item.ImageManager.Title} 다운로드 취소");
                if (Application.Current.Dispatcher.CheckAccess()) DownloadQueue.Remove(item);
                else Application.Current.Dispatcher.Invoke(() => DownloadQueue.Remove(item));
                return;
            }
            else
            {
                var formattedTitle = Regex.Replace(item.ImageManager.Title, "[\\\\/:*?\"<>|\\s]", "_");
                var Path = $"{Common.Setting.DownloadPath}\\{formattedTitle}";
                if (Common.Setting.isCompress)
                {
                    await CompleteAndCompressFile($"{Path}.zip", item);
                }
                else
                {
                    await CompletedFolder(Path, item);
                }
                NotificationManager.NotifySuccess($"{item.ImageManager.Title} 다운로드 완료.");
                if (Application.Current.Dispatcher.CheckAccess()) DownloadQueue.Remove(item);
                else Application.Current.Dispatcher.Invoke(() => DownloadQueue.Remove(item));
            }
        }
        private async Task CompletedFolder(string Path,DownloadItem item)
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
            using (item)
            {
                var CompletedQueue = item.Completed;
                DownloadResult CompletedItem = null;
                while (CompletedQueue.TryDequeue(out CompletedItem))
                {
                    var image = CompletedItem.Source as MetadataImage;
                    using (var fs = new FileStream($"{Path}/{image.Name}", FileMode.Create))
                    {
                        using (CompletedItem.Data)
                        {
                            await CompletedItem.Data.CopyToAsync(fs);
                        }
                    }
                }
            }
        }
        private async Task CompleteAndCompressFile(string FileName,DownloadItem item)
        {
            DownloadResult CompletedItem = null;
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var CompletedQueue = item.Completed;
                    while (CompletedQueue.TryDequeue(out CompletedItem))
                    {
                        var image = CompletedItem.Source as MetadataImage;
                        var fileInArchive = archive.CreateEntry(image.Name);
                        using (var entryStream = fileInArchive.Open())
                        {
                            using (CompletedItem.Data)
                            {
                                await CompletedItem.Data.CopyToAsync(entryStream);
                            }
                        }
                    }
                }
                using (var fs = new FileStream(FileName, FileMode.Create))
                {
                    var compressedBytes = memoryStream.ToArray();
                    await fs.WriteAsync(compressedBytes, 0, compressedBytes.Length);
                }

            }
        }
        public static DownloadManager Instance => _instance.Value;
    }
}
