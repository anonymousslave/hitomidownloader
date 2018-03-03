using System;

namespace hitomiDownloader.Models
{
    public enum Then
    {
        Include,
        Exclude
    }
    public enum JoinRule
    {
        And,
        Or
    }
    public class Query : IEquatable<Query>
    {
        public JoinRule Join { get; set; }
        public Then Then { get; set; }
        public string Tag { get; set; }
        public bool Equals(Query other)
        {
            return Then == other.Then && Join == other.Join && Tag.Equals(other.Tag);
        }

        public override string ToString()
        {
            return $"{Tag}";
        }
    }
}
