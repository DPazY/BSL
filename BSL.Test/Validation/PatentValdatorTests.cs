using BSL.Implementation.Validator;
using BSL.Models;
using FluentValidation.TestHelper;

namespace BSL.Test.Validators
{
    [TestFixture]
    public class PatentValidatorTests
    {
        private PatentValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new PatentValidator();
        }

        [Test]
        public void Should_Have_Errors_When_Required_String_Fields_Are_Empty()
        {
            var model = new Patent("", "", "", null!, new DateOnly(2000, 1, 1), new DateOnly(2001, 1, 1), 10, null);
            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(p => p.Name);
            result.ShouldHaveValidationErrorFor(p => p.Inventor);
            result.ShouldHaveValidationErrorFor(p => p.Country);
        }

        [TestCase(1949)]
        [TestCase(1800)]
        public void Should_Have_Error_When_Submission_Year_Is_Less_Than_1950(int invalidYear)
        {
            var model = new Patent("Патент", "Изобретатель", "Страна", "123",
                new DateOnly(invalidYear, 1, 1), new DateOnly(2000, 1, 1), 10, null);
            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor("SubmissionDate.Year");
        }

        [TestCase(1949)]
        public void Should_Have_Error_When_Publication_Year_Is_Less_Than_1950(int invalidYear)
        {
            var model = new Patent("Патент", "Изобретатель", "Страна", "123",
                new DateOnly(1950, 1, 1), new DateOnly(invalidYear, 1, 1), 10, null);
            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor("PublicationDate.Year");
        }

        [Test]
        public void Should_Have_Error_When_Publication_Date_Is_Before_Submission_Date()
        {
            var model = new Patent("Патент", "Изобретатель", "Страна", "123",
                new DateOnly(2005, 1, 1), new DateOnly(2000, 1, 1), 10, null);
            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor("PublicationDate.Year");
            result.ShouldHaveValidationErrorFor(p => p.PublicationDate);
        }

        [Test]
        public void Should_Not_Have_Error_When_Dates_Are_Valid()
        {
            var model = new Patent("Патент", "Изобретатель", "Страна", "123",
                new DateOnly(2000, 1, 1), new DateOnly(2001, 1, 1), 10, null);
            var result = _validator.TestValidate(model);

            result.ShouldNotHaveValidationErrorFor("SubmissionDate.Year");
            result.ShouldNotHaveValidationErrorFor("PublicationDate.Year");
            result.ShouldNotHaveValidationErrorFor(p => p.PublicationDate);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Should_Have_Error_When_NumberOfPages_Is_Zero_Or_Less(int invalidPages)
        {
            var model = new Patent("Патент", "Изобретатель", "Страна", "123",
                new DateOnly(2000, 1, 1), new DateOnly(2001, 1, 1), invalidPages, null);
            var result = _validator.TestValidate(model);

            result.ShouldHaveValidationErrorFor(p => p.NumberOfPages);
        }
    }
}