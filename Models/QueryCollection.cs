using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hitomiDownloader.Models
{
    public class QueryCollection : ReactiveList<Query>
    {
        public Func<Task> Search;
        public bool IsExcluded { get; set; }
        public bool IsJoin { get; set; }
        public QueryCollection(IEnumerable<Query> queries) :base(queries)
        {
        }
        public QueryCollection() : this(Enumerable.Empty<Query>()) {}

        public void AddQuery(IEnumerable<Query> queries)
        {
            queries.ToObservable()
                .Where(x => !TagContains(x))
                .Subscribe(Add, async ()=>await Search());
        }
        public void AddQuery(string query)
        {
            AddQuery(new[] { query });
        }
        public void AddQuery(params string[] query)
        {
            AddQuery(ParseQuery(query));
        }
        public async Task RemoveQuery(Query tag)
        {
            Remove(tag);
            if (Search != null) await Search();
        }
        /// <summary>
        /// 입력된 쿼리 조건을 이용하여, 쿼리의 목록을 생성
        /// </summary>
        /// <remarks>태그에 대한 조건만 등록가능</remarks>
        /// <returns></returns>
        public IEnumerable<Query> ParseQuery(params string[] tags)
        {
            return tags.Select(tag => new Query()
            {
                Then = IsExcluded ? Then.Exclude : Then.Include,
                Join = IsJoin ? JoinRule.Or : JoinRule.And,
                Tag = tag
            });
        }

        public string WhereClause()
        {
            List<string> include = new List<string>();
            List<string> exclude = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach(var tag in this.Where(x=>x.Then == Then.Include))
            {
                builder = new StringBuilder(include.Count != 0 ? $"{tag.Join.ToString()} " : $"");
                builder.Append($"Tags LIKE '%{tag.Tag}%'");
                include.Add(builder.ToString());

            }
            foreach (var tag in this.Where(x => x.Then == Then.Exclude))
            {
                builder = new StringBuilder(exclude.Count != 0 ? $"{tag.Join.ToString()} " : $"");
                builder.Append($"Tags NOT LIKE '%{tag.Tag}%'");
                exclude.Add(builder.ToString());
            }
            var includes = string.Join(" ", include);
            var excludes = string.Join(" ", exclude);
            builder = new StringBuilder();
            if (!string.IsNullOrEmpty(includes))
            {
                builder.Append($"({includes})");
                if (!string.IsNullOrEmpty(excludes))
                {
                    builder.Append($" and ({excludes})");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(excludes))
                {
                    builder.Append($"({excludes})");
                }
            }

            return builder.ToString();
        }
        /// <summary>
        /// 태그가 해당 메타데이터에 포함되었는지 확인
        /// </summary>
        /// <remarks>Negative 모드일때 해당 조건이 포함되었다면 무조건 False</remarks>
        /// <returns></returns>
        public bool TagContains(object o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            var ret = false;
            if ((o is Query query))
            {
                return this.Contains(query);
            }
            else return ret;


        }
    }
}
