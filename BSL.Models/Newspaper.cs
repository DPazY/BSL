using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BSL.Models
{
    [ProtoContract]
    public record Newspaper : Edition
    {
        protected Newspaper() : base("") { }
        [SetsRequiredMembers]
        public Newspaper(string name,string placeOfPublication, string publishingHouse, int numberOfPages, string? notes, uint issueNumber, DateOnly dataPublishing, string? issn) : base(name)
        {
            PlaceOfPublication = placeOfPublication;
            PublishingHouse = publishingHouse;
            NumberOfPages = numberOfPages;
            Notes = notes;
            IssueNumber = issueNumber;
            if (dataPublishing.Year >= 1900) DataPublishing = dataPublishing;
            else throw new ArgumentOutOfRangeException();
            ISSN = issn;
        }
        [ProtoMember(1)]
        public string PlaceOfPublication { get; init; }
        [ProtoMember(2)]
        public required string PublishingHouse { get; init; }
        [ProtoMember(3)]
        public int NumberOfPages { get; init; }
        [ProtoMember(4)]
        public string? Notes { get; init; }
        [ProtoMember(5)]
        public uint IssueNumber { get; init; }
        [ProtoMember(6)]
        public required DateOnly DataPublishing {  get; init; }
        [ProtoMember(7)]
        public string? ISSN { get; init; }

    }
}

