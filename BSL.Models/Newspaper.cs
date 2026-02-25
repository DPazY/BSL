using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BSL.Models
{
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
        public string PlaceOfPublication { get; init; }
        public required string PublishingHouse { get; init; }
        public int NumberOfPages { get; init; }
        public string? Notes { get; init; }
        public uint IssueNumber { get; init; }
        public required DateOnly DataPublishing {  get; init; }
        public string? ISSN { get; init; }

    }
}

