using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using hitomiDownloader.Models;
using Newtonsoft.Json;
using System.Reactive.Linq;
using System.Collections.Concurrent;
using System.Linq;

namespace hitomiDownloader.Core
{
    public static class Common
    {
        public static HitomiSetting Setting { get; set; }

        public static bool ContainsString(this string str,string sub)=>(str.Length - str.Replace(sub, string.Empty).Length) / sub.Length > 0;
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop,Func<T,Task> body)
        {
            if (source != null)
            {
                return Task.WhenAll(
                    from partition in Partitioner.Create(source).GetPartitions(dop)
                    select Task.Run(async delegate
                    {
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                await body(partition.Current);
                            }
                        }
                    }));
            }
            else
            {
                return null;
            }
        }
        public static void DisposeSequence<T>(this IEnumerable<T> source)
        {
            foreach (var disposableObject in source.OfType<IDisposable>())
            {
                disposableObject.Dispose();
            };
        }
        public static async Task<T> GetJsonObject<T>(string path)
        {
            using (var fs = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                return JsonConvert.DeserializeObject<T>(await fs.ReadToEndAsync());
            }
        }
        public static async void SetJsonObject<T>(string path,T value)
        {
            var json = JsonConvert.SerializeObject(value,Formatting.Indented);
            using (var fs = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write)))
            {
                await fs.WriteAsync(json);
            }
        }
    }
}
