using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public record class Newspaper : Editions
    {
        public Newspaper(string name, string? placeOfPublication, string publishingHouse, int yearOfPublication, 
                        int? numberOfPages, string? notes, int issueNumber, DateOnly date, string? issn = null) : base(name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
                
            if (string.IsNullOrWhiteSpace(publishingHouse))
                throw new ArgumentException("Publishing house cannot be empty", nameof(publishingHouse));
                
            if (yearOfPublication < 1900)
                throw new ArgumentOutOfRangeException(nameof(yearOfPublication), "Year of publication must be 1900 or later");
                
            if (issueNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(issueNumber), "Issue number must be positive");

            Name = name;
            PlaceOfPublication = placeOfPublication;
            PublishingHouse = publishingHouse;
            YearOfPublication = (uint)yearOfPublication;
            NumberOfPages = numberOfPages;
            Notes = notes;
            IssueNumber = issueNumber;
            if (date.Year >= 1950) DataPublishing = date;
            else throw new ArgumentOutOfRangeException(nameof(date), "Date of publication must be 1950 or later");
            ISSN = issn;
        }

        public string? PlaceOfPublication { get; init; }
        public string PublishingHouse { get; init; }
        public uint YearOfPublication { get; init; }
        public int? NumberOfPages { get; init; }
        public string? Notes { get; init; }
        public int IssueNumber { get; init; }
        public DateOnly DataPublishing { get; init; }
        public string? ISSN { get; init; }
    }
}

