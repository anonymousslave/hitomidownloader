using Dapper;
using Dapper.Contrib.Extensions;
using hitomiDownloader.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hitomiDownloader.Core
{
    public class DatabaseManager
    {
        private static readonly Lazy<DatabaseManager> _instance = new Lazy<DatabaseManager>(() => new DatabaseManager());
        string dbPath = $"{Environment.CurrentDirectory}\\hitomi.db";
        SQLiteConnection dbConnection = null;
        public SQLiteConnection Connection => dbConnection;
        private DatabaseManager()
        {

        }
        public async Task InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
            dbConnection = new SQLiteConnection($"Data Source={dbPath};Vertison=3");
            await dbConnection.OpenAsync();
            await dbConnection.ExecuteAsync("CREATE TABLE if not exists Tag (ID integer primary key not null, Content varchar)");
            await dbConnection.ExecuteAsync("CREATE TABLE if not exists Metadata ( ID INTEGER NOT NULL, MetadataID integer, Artist varchar, Name varchar, Language varchar, Type varchar, Tags varchar, PRIMARY KEY(ID) )");
            await dbConnection.ExecuteAsync("CREATE TABLE if not exists MetadataImage ( ID INTEGER NOT NULL, ImageID integer,Name varchar, Url varchar, MetadataID INTEGER NOT NULL, PRIMARY KEY(ID) )");
        }
        public async Task UpdateTags()
        {
            if (dbConnection == null) return;
            var json = await RequestManager.Instance.DownloadAs<TagdataCollection>("https://ltn.hitomi.la/tags.json");
            using (var transaction = dbConnection.BeginTransaction())
            {
                await dbConnection.DeleteAllAsync<Tag>(transaction);
                await dbConnection.InsertAsync(json.ToTag(), transaction);
                transaction.Commit();
            }
        }
        public async Task UpdateMetadata()
        {
            if (dbConnection == null) return;
            using (var transaction = dbConnection.BeginTransaction())
            {
                await dbConnection.DeleteAllAsync<Metadata>(transaction);
                await dbConnection.DeleteAllAsync<MetadataImage>(transaction);
                await Task.WhenAll(Enumerable.Range(0, Common.Setting.number_of_gallery_jsons).Select(no => RequestManager.Instance.DownloadAs<IEnumerable<JsonMetadata>>($"https://ltn.hitomi.la/galleries{no}.json").ContinueWith(async t =>
                {
                    var items = t.Result;
                    await dbConnection.InsertAsync(
                    items.Select(x => new Metadata()
                    {
                        MetadataID = x.ID,
                        Artist = string.Join(",", x.Artists ?? Enumerable.Empty<string>()),
                        Name = x.Name,
                        Language = x.Language,
                        Type = x.Type,
                        Tags = x.MetadataTag
                    }).ToArray(), transaction);

                })));
                transaction.Commit();
            }
        }

        public async Task UpdateMetadataImage(int ID)
        {
            if (dbConnection == null) return;
            using (var transaction = dbConnection.BeginTransaction())
            {

                await RequestManager.Instance.DownloadAs<JsonImageData[]>($"https://hitomi.la/galleries/{ID}.js", s => s.Replace("var galleryinfo = ", ""))
                .ContinueWith(async t =>
                {
                    var items = t.Result;
                    await dbConnection.InsertAsync(items.Select((item, i) =>
                    {
                        var subDomain = Convert.ToChar(97 + (ID % 2));
                        return new MetadataImage()
                        {
                            ImageID = i,
                            Name = item.Name,
                            Url = $"https://{subDomain}a.hitomi.la/galleries/{ID}/{item.Name}",
                            MetadataID = ID
                        };
                    }).ToArray(), transaction);
                });
                transaction.Commit();
            }
        }
        public async Task<IEnumerable<Tag>> GetTags(string where)=>
            await dbConnection?.QueryAsync<Tag>($"select * from Tag where {where}") ?? Enumerable.Empty<Tag>();
        public async Task<IEnumerable<Metadata>> GetMetadata(string where = null) =>
            await dbConnection?.QueryAsync<Metadata>(where) ?? Enumerable.Empty<Metadata>();
        public async Task<IEnumerable<MetadataImage>> GetMetadataImage(int ID)=>
            await dbConnection?.QueryAsync<MetadataImage>($"select * from MetadataImage where MetadataID = {ID}")
            ?? Enumerable.Empty<MetadataImage>();
        public static DatabaseManager Instance => _instance.Value;
    }
}
