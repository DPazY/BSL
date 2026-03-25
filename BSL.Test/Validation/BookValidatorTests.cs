using BSL.Implementation.Validator;
using BSL.Models;
using FluentValidation.TestHelper;

namespace BSL.Test.Validators
{
    [TestFixture]
    public class BookValidatorTests
    {
        private BookValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new BookValidator();
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new Book("", new DateOnly(2000, 1, 1), "Издатель", "Автор");
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(b => b.Name);
        }

        [TestCase(1949)]
        [TestCase(1000)]
        public void Should_Have_Error_When_YearBook_Is_Less_Than_1950(int invalidYear)
        {
            var model = new Book("Название", new DateOnly(invalidYear, 1, 1), "Издатель", "Автор");
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(b => b.YearBook);
        }

        [TestCase(1950)]
        [TestCase(2023)]
        public void Should_Not_Have_Error_When_YearBook_Is_Valid(int validYear)
        {
            var model = new Book("Название", new DateOnly(validYear, 1, 1), "Издатель", "Автор");
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(b => b.YearBook);
        }

        [Test]
        public void Should_Have_Error_When_Author_List_Contains_Empty_String()
        {
            var model = new Book("Название", new DateOnly(2000, 1, 1), "Издатель", "Автор, ,Другой автор");

            model = model with { Author = new List<string> { "Автор 1", "", null! } };

            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(b => b.Author);
        }
    }
}