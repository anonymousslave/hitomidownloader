using System;
using System.Linq;
using System.Reactive.Linq;
using hitomiDownloader.Core;
using ReactiveUI;
using System.Threading.Tasks;
using Dapper;
using System.Text;

namespace hitomiDownloader.Models
{
    public class MetadataCollection : ReactiveList<MetadataModel>
    {
        public ReactiveCommand FirstCommand { get; }
        public ReactiveCommand PrevCommand { get; }
        public ReactiveCommand NextCommand { get; }
        public ReactiveCommand LastCommand { get; }
        public ReactiveCommand DownloadCommand { get; }
        private int currentPage;
        public int CurrentPage { get { return currentPage; } set { this.RaiseAndSetIfChanged(ref currentPage, value); } }
        public int TotalPages { get; private set; }
        private readonly QueryCollection queries;
        private string Clause => queries.WhereClause();
        public MetadataCollection(QueryCollection queries)
        {
            this.queries = queries;
            this.queries.Search = Search;
            CurrentPage = 1;
            TotalPages = 1;
            FirstCommand = ReactiveCommand.CreateFromTask(ShowFirstPage);
            NextCommand = ReactiveCommand.CreateFromTask(ShowNextPage,this.WhenAny(x=>x.CurrentPage,x=>x.Value <= TotalPages));
            PrevCommand = ReactiveCommand.CreateFromTask(ShowPreviousPage, this.WhenAny(x => x.CurrentPage, x => x.Value > 1));
            LastCommand = ReactiveCommand.CreateFromTask(ShowLastPage);
            DownloadCommand = ReactiveCommand.Create(DownloadPage);

        }
        public async Task Search()
        {
            var query = new StringBuilder($"select Count(*) from Metadata");
            if (!string.IsNullOrEmpty(Clause)) query.Append($" where {Clause}");
            var count = await DatabaseManager.Instance.Connection.QueryFirstAsync<int>(query.ToString());
            if (count % Common.Setting.max_number_of_results == 0)
                TotalPages = (count / Common.Setting.max_number_of_results);
            else
            {
                TotalPages = (count / Common.Setting.max_number_of_results) + 1;
            }
            await ShowFirstPage();
        }
        private async Task Fetch(int page)
        {
            this.DisposeSequence();
            Clear();
            StringBuilder query =  new StringBuilder($"select * from Metadata");
            if (!String.IsNullOrEmpty(Clause)) query.Append($" where {Clause}");
            query.Append($" LIMIT {Common.Setting.max_number_of_results} OFFSET {Common.Setting.max_number_of_results * (CurrentPage - 1)}");
            var items = await DatabaseManager.Instance.Connection.QueryAsync<Metadata>(query.ToString());
            items.ToObservable()
                .Select((x, index) => new MetadataModel(x, index))
                .Do(x=> {
                    x.TagClick = ReactiveCommand.Create<string>(queries.AddQuery);
                })
                .Subscribe(Add);
            this.RaisePropertyChanged(nameof(CurrentPage));
            this.RaisePropertyChanged(nameof(TotalPages));
            GC.Collect();
        }
        private async Task ShowFirstPage()
        {
            CurrentPage = 1;
            await Fetch(CurrentPage);
        }
        private async Task ShowNextPage()
        {
            CurrentPage++;
            await Fetch(CurrentPage);
        }

        private async Task ShowPreviousPage()
        {
            CurrentPage--;
            await Fetch(CurrentPage);
        }
        private async Task ShowLastPage()
        {
            CurrentPage = TotalPages;
            await Fetch(CurrentPage);
        }
        private void DownloadPage()
        {
            foreach(MetadataModel item in this)
            {
                DownloadManager.Instance.Enqueue(item.imageManager);
            }
        }
    }
}
