using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public record class Book : Editions
    {
        public Book(string name, DateOnly year, string publisher, string author) : base(name)
        {
            if (year.Year >= 1900) YearBook = (uint)year.Year;
            else throw new ArgumentOutOfRangeException(nameof(year));
            PublisherBook = publisher;
            Author = author.Split(',', ' ', ';').Where(auth =>
            !string.IsNullOrWhiteSpace(auth) && !string.IsNullOrWhiteSpace(auth)).ToList();
        }

        public uint YearBook { get; init; }
        public string PublisherBook { get; init; }
        public List<string> Author { get; init; }
    }
}