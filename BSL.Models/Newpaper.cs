using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public record Newspaper : Editions
    {
        public Newspaper(string name, string? placeOfPublication, string publishingHouse, 
                        int? numberOfPages, string? notes, int issueNumber, DateOnly date, string? issn = null) : base(name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
                
            if (string.IsNullOrWhiteSpace(publishingHouse))
                throw new ArgumentException("Publishing house cannot be empty", nameof(publishingHouse));
                
            if (issueNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(issueNumber), "Issue number must be positive");

            Name = name;
            PlaceOfPublication = placeOfPublication;
            PublishingHouse = publishingHouse;
            NumberOfPages = numberOfPages;
            Notes = notes;
            IssueNumber = issueNumber;
            if (date.Year >= 1950) DataPublishing = date;
            else throw new ArgumentOutOfRangeException();
            ISSN = iSSN;
        }

        public string? PlaceOfPublication { get; init; }
        public string PublishingHouse { get; init; }
        public int? NumberOfPages { get; init; }
        public string? Notes { get; init; }
        public int IssueNumber { get; init; }
        public DateOnly DataPublishing { get; init; }
        public string? ISSN { get; init; }
    }
}

