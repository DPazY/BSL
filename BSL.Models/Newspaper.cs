using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public record Newspaper : Editions
    {
        public string PlaceOfPublication { get; private set; }
        public string PublishingHouse { get; private set; }
        public int NumberOfPages { get; private set; }
        public string? Notes { get; private set; }
        public int IssueNumber { get; private set; }
        public DateOnly DataPublishing {  get; private set; }
        public string? ISSN { get; private set; }

        public Newspaper(string name,string placeOfPublication, string publishingHouse, int numberOfPages, string? notes, int issueNumber, DateOnly date, string? issn) : base(name)
        {
            PlaceOfPublication = placeOfPublication;
            PublishingHouse = publishingHouse;
            NumberOfPages = numberOfPages;
            Notes = notes;
            IssueNumber = issueNumber;
            if (date.Year >= 1950) DataPublishing = date;
            else throw new ArgumentOutOfRangeException();
            ISSN = issn;
        }
    }
}

