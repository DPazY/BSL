using BSL.Models;
using FluentValidation;

namespace BSL.Implementation.Validator
{
    public class EditionValidator : AbstractValidator<Edition>
    {
        public EditionValidator() 
        {
            RuleFor(e => e.Name).NotEmpty();
        }
    }
}
