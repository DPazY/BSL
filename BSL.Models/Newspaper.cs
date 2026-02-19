using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BSL.Models
{
    public record Newspaper : Edition
    {
        public string PlaceOfPublication { get; private set; }
        public string PublishingHouse { get; private set; }
        public int NumberOfPages { get; private set; }
        public string? Notes { get; private set; }
        public uint IssueNumber { get; private set; }
        public required DateOnly DataPublishing {  get; init; }
        public string? ISSN { get; private set; }

        [SetsRequiredMembers]
        public Newspaper(string name,string placeOfPublication, string publishingHouse, int numberOfPages, string? notes, uint issueNumber, DateOnly dataPublishing, string? issn) : base(name)
        {
            PlaceOfPublication = placeOfPublication;
            PublishingHouse = publishingHouse;
            NumberOfPages = numberOfPages;
            Notes = notes;
            IssueNumber = issueNumber;
            if (dataPublishing.Year >= 1950) DataPublishing = dataPublishing;
            else throw new ArgumentOutOfRangeException();
            ISSN = issn;
        }
    }
}

