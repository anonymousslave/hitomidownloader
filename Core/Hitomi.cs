using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using hitomiDownloader.Models;
using ReactiveUI;
using System.Collections.Generic;
using ToastNotifications;
using ToastNotifications.Position;
using ToastNotifications.Lifetime;
using System.Windows;

namespace hitomiDownloader.Core
{
    public partial class Hitomi : ReactiveObject
    {
        public QueryCollection Queries { get; set; }
        public MetadataCollection Galleries { get; }

        private string _searchQuery;
        public string SearchQuery { get { return _searchQuery; } set { this.RaiseAndSetIfChanged(ref _searchQuery, value); } }

        public ReactiveList<string> SearchResults { get; }
        public ReactiveCommand<string, IEnumerable<Tag>> SearchTag { get; private set; }

        public ReactiveCommand UpdateCache { get; }
        public ReactiveCommand SaveSettings { get; }
        public ReactiveCommand AddTag { get; }
        public ReactiveCommand RemoveTag { get; }
        private bool _isFiltered;
        public bool IsFiltered { get { return _isFiltered; } set { this.RaiseAndSetIfChanged(ref _isFiltered, value); } }
        public int MaxResult => Common.Setting?.max_number_of_results ?? 0;
        public Hitomi()
        {
            Queries = new QueryCollection();
            Galleries = new MetadataCollection(Queries);

            SearchResults = new ReactiveList<string>();

            SearchTag = ReactiveCommand.CreateFromTask<string, IEnumerable<Tag>>(async _ => {
                return await DatabaseManager.Instance.GetTags($"Content LIKE '%{SearchQuery}%'");
            } ,this.WhenAny(x=>x.SearchQuery,x=>!string.IsNullOrWhiteSpace(x.Value)));
            SearchTag.Subscribe(x =>
            {
                SearchResults.Clear();
                x.ToObservable().Select(o=>o.Content).Subscribe(SearchResults.Add);
            });
            this.WhenAnyValue(x => x.SearchQuery)
            .Throttle(TimeSpan.FromSeconds(1), RxApp.MainThreadScheduler)
            .InvokeCommand(SearchTag);
            InitializeSetting();

            UpdateCache = ReactiveCommand.CreateFromTask(UpdateGalleryCache);

            AddTag = ReactiveCommand.Create<string>(Queries.AddQuery);
            RemoveTag = ReactiveCommand.CreateFromTask<Query>(Queries.RemoveQuery);
            SaveSettings = ReactiveCommand.Create(SaveSetting);
        }
        private async void InitializeSetting()
        {
            if (!File.Exists("Setting.json"))
            {
                Common.Setting = new HitomiSetting
                {
                    DownloadPath = "./Download",
                    Queries = new Query[] { new Query() { Then = Then.Include, Tag = "language:korean" }, new Query() { Then = Then.Exclude, Tag = "tag:webtoon" } },
                    max_number_of_results = 10,
                    preload_number = 5,
                    number_of_gallery_jsons =20,
                    isCompress = true,
                };
                Common.SetJsonObject("Setting.json", Common.Setting);
            }
            Common.Setting = await Common.GetJsonObject<HitomiSetting>("Setting.json");
            if(Common.Setting.Queries.Length != 0)
                Common.Setting.Queries.ToObservable().Subscribe(Queries.Add);
        }
        public void SaveSetting()
        {
            Common.Setting.Queries = Queries.ToArray();
            Common.SetJsonObject("Setting.json",Common.Setting);
        }
    }
}
