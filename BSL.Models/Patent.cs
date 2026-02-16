namespace BSL.Models
{
    public class Patent : Editions
    {
        public Patent(string name,string inventor,  DateOnly submissionDate, DateOnly publicationDate) : base(name)
        {
            if (!string.IsNullOrEmpty(inventor) && !string.IsNullOrWhiteSpace(inventor)) Inventor = inventor;
            else throw new ArgumentException();

            if (submissionDate.Year >= 1950) SubmissionDate = submissionDate;
            else throw new ArgumentOutOfRangeException();
            
            if (publicationDate.Year >= 1950) PublicationDate = publicationDate;
            else throw new ArgumentOutOfRangeException();
        }
        public string Inventor {  get; init; }
        public DateOnly SubmissionDate {  get; init; }
        public DateOnly PublicationDate {  get; init; }
    }
}