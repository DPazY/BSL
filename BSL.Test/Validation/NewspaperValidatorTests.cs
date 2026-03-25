using BSL.Implementation.Validator;
using BSL.Models;
using FluentValidation.TestHelper;

namespace BSL.Test.Validators
{
    [TestFixture]
    public class NewspaperValidatorTests
    {
        private NewspaperValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new NewspaperValidator();
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new Newspaper("", "Место", "Издательство", 10, null, 1, new DateOnly(2000, 1, 1), "123");
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(n => n.Name);
        }

        [TestCase(1900)]
        [TestCase(1899)]
        public void Should_Have_Error_When_Publishing_Year_Is_1900_Or_Less(int invalidYear)
        {
            var model = new Newspaper("Газета", "Место", "Издательство", 10, null, 1, new DateOnly(invalidYear, 1, 1), "123");
            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor("DataPublishing.Year");
        }

        [TestCase(1901)]
        [TestCase(2024)]
        public void Should_Not_Have_Error_When_Publishing_Year_Is_Greater_Than_1900(int validYear)
        {
            var model = new Newspaper("Газета", "Место", "Издательство", 10, null, 1, new DateOnly(validYear, 1, 1), "123");
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor("DataPublishing.Year");
        }
    }
}