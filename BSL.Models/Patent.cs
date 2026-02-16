namespace BSL.Models
{
    public record class Patent : Editions
    {
        public string Inventor { get; private set; }
        public string Country { get; private set; }
        public string RegistrationNumber { get; private set; }
        public DateOnly SubmissionDate { get; private set; }
        public DateOnly PublicationDate { get; private set; }
        public int NumberOfPages { get; private set; }
        public string? Notes { get; private set; }

        public Patent(
            string name,
            string inventor,
            string country,
            string registrationNumber,
            DateOnly submissionDate,
            DateOnly publicationDate,
            int numberOfPages,
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
    }
}