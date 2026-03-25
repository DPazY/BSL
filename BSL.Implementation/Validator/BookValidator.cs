using BSL.Models;
using FluentValidation;

namespace BSL.Implementation.Validator
{
    public class BookValidator : AbstractValidator<Book>
    {
        public BookValidator()
        {
            RuleFor(b => b.Name).NotEmpty();
            RuleFor(b => b.YearBook).GreaterThanOrEqualTo(1950);
            RuleFor(b => b.Author).ForEach(auth =>
            auth
                .NotNull()
                .NotEmpty());
        }
    }
}
