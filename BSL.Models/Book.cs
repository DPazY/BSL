using System;
using System.Collections.Generic;
using System.Linq;

namespace BSL.Models
{
    public record class Book : Editions
    {
        public Book(string name, DateOnly year, string publisher, string author, string? isbn = null, 
                   string? placeOfPublication = null, int? pages = null, string? notes = null) : base(name)
        {
            if (year.Year >= 1900) YearBook = (uint)year.Year;
            else throw new ArgumentOutOfRangeException(nameof(year));
            
            if (string.IsNullOrWhiteSpace(publisher))
                throw new ArgumentException("Publisher cannot be empty", nameof(publisher));
                
            if (string.IsNullOrWhiteSpace(author))
                throw new ArgumentException("Author cannot be empty", nameof(author));
                
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            PublisherBook = publisher;
            PlaceOfPublication = placeOfPublication;
            Author = author.Split(new char[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(a => a.Trim())
                          .Where(auth => !string.IsNullOrWhiteSpace(auth)).ToList();
            ISBN = isbn;
            Pages = pages;
            Notes = notes;
        }

        public uint YearBook { get; init; }
        public string PublisherBook { get; init; }
        public string? PlaceOfPublication { get; init; }
        public List<string> Author { get; init; }
        public string? ISBN { get; init; }
        public int? Pages { get; init; }
        public string? Notes { get; init; }
    }
}