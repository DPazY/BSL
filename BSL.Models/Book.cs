using ProtoBuf;
using System.Diagnostics.CodeAnalysis;

namespace BSL.Models
{
    [ProtoContract]
    public record Book : Edition
    {
        protected Book() : base("") { }
        [SetsRequiredMembers]
        public Book(string name, DateOnly year, string publisher, string author) : base(name)
        {
            YearBook = year.Year;
            PublisherBook = publisher;
            Author = author.Split(',', ';').ToList();
        }
        [ProtoMember(1)]
        public int YearBook { get; init; }
        [ProtoMember(2)]
        public required string PublisherBook { get; init; }
        [ProtoMember(3)]
        public required List<string> Author { get; init; }
    }
}