using BSL.Models;
using FluentValidation;

namespace BSL.Implementation.Validator
{
    public class NewspaperValidator : AbstractValidator<Newspaper>
    {
        public NewspaperValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(n => n.DataPublishing.Year).NotNull().GreaterThan(1900);
        }
    }
}
