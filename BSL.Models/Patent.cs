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
            int numberOfPages,
            string? notes) : base(name)
        {
            Inventor = inventor;
            Country = country;
            RegistrationNumber = registrationNumber;
            SubmissionDate = submissionDate;
            PublicationDate = publicationDate;
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
        public int NumberOfPages { get; init; }
        [ProtoMember(7)]
        public string? Notes { get; init; }

    }
}