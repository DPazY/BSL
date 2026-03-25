using BSL.Models;
using FluentValidation;

namespace BSL.Implementation.Validator
{
    public class PatentValidator : AbstractValidator<Patent>
    {
        public PatentValidator()
        {
            RuleFor(p => p.Name).NotEmpty();
            RuleFor(p => p.Inventor).NotEmpty();
            RuleFor(p => p.Country).NotEmpty();
            RuleFor(p => p.SubmissionDate.Year).NotEmpty()
                .GreaterThanOrEqualTo(1950);
            RuleFor(p => p.PublicationDate.Year).NotEmpty()
                .GreaterThanOrEqualTo(1950)
                .GreaterThanOrEqualTo(p => p.SubmissionDate.Year);
            RuleFor(p => p.PublicationDate)
                .GreaterThanOrEqualTo(p => p.SubmissionDate);
            RuleFor(p => p.NumberOfPages)
                .GreaterThan(0);
        }
    }
}
