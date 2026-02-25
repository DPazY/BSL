using ProtoBuf;
using System.Diagnostics.CodeAnalysis;

namespace BSL.Models
{
    [ProtoContract]
    public record Patent : Edition
    {
        protected Patent() : base("") { }

        [SetsRequiredMembers]
        public Patent(
            string name,
            string inventor,
            string country,
            string registrationNumber,
            DateOnly submissionDate,
            DateOnly publicationDate,
            uint numberOfPages,
            string? notes) : base(name)
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
                throw new ArgumentOutOfRangeException(nameof(submissionDate), "Дата подачи заявки не может быть ранее 1950 года");
            SubmissionDate = submissionDate;

            if (publicationDate.Year < 1950)
                throw new ArgumentOutOfRangeException(nameof(publicationDate), "Дата публикации не может быть ранее 1950 года");
            PublicationDate = publicationDate;

            if (publicationDate < submissionDate)
                throw new ArgumentException("Дата публикации не может быть раньше даты подачи заявки");

            if (numberOfPages <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfPages), "Количество страниц должно быть больше 0");
            NumberOfPages = numberOfPages;

            Notes = notes;
        }

        [ProtoMember(1)]
        public required string Inventor { get; init; }
        [ProtoMember(2)]
        public required string Country { get; init; }
        [ProtoMember(3)]
        public string RegistrationNumber { get; init; }
        [ProtoMember(4)]
        public DateOnly SubmissionDate { get; init; }
        [ProtoMember(5)]
        public DateOnly PublicationDate { get; init; }
        [ProtoMember(6)]
        public uint NumberOfPages { get; init; }
        [ProtoMember(7)]
        public string? Notes { get; init; }

    }
}