using System;
using System.Collections.Generic;
using System.Text;

namespace BSL.Models
{
    public record class Patent : Editions
    {
        public Patent(
            string name,
            string inventor,
            string country,
            string registrationNumber,
            DateOnly submissionDate,
            DateOnly publicationDate,
            int? numberOfPages = null,
            string? notes = null) : base(name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название патента не может быть пустым", nameof(name));

            if (string.IsNullOrWhiteSpace(inventor))
                throw new ArgumentException("Изобретатель не может быть пустым", nameof(inventor));
            Inventor = inventor;

            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Страна не может быть пустой", nameof(country));
            Country = country;

            RegistrationNumber = registrationNumber ?? throw new ArgumentNullException(nameof(registrationNumber));

            if (submissionDate.Year < 1950)
                throw new ArgumentOutOfRangeException(nameof(submissionDate), "Дата подачи заявки не может быть earlier 1950 года");
            SubmissionDate = submissionDate;

            if (publicationDate.Year < 1950)
                throw new ArgumentOutOfRangeException(nameof(publicationDate), "Дата публикации не может быть earlier 1950 года");
            PublicationDate = publicationDate;

            if (publicationDate < submissionDate)
                throw new ArgumentException("Дата публикации не может быть раньше даты подачи заявки");

            NumberOfPages = numberOfPages ?? 0;
            Notes = notes;
        }

        public string Inventor { get; init; }
        public string Country { get; init; }
        public string RegistrationNumber { get; init; }
        public DateOnly SubmissionDate { get; init; }
        public DateOnly PublicationDate { get; init; }
        public int? NumberOfPages { get; init; }
        public string? Notes { get; init; }
    }
}