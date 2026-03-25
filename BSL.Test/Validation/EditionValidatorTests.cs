using BSL.Implementation.Validator;
using BSL.Models;
using FluentValidation.TestHelper;
using System.Diagnostics.CodeAnalysis;

namespace BSL.Test.Validators
{
    [TestFixture]
    public class EditionValidatorTests
    {
        private EditionValidator _validator;

        private record TestEdition : Edition
        {
            [SetsRequiredMembers]
            public TestEdition(string name) : base(name) { }
        }

        [SetUp]
        public void Setup()
        {
            _validator = new EditionValidator();
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Null()
        {
            var model = new TestEdition(null!);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(e => e.Name);
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new TestEdition("");
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(e => e.Name);
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Is_Valid()
        {
            var model = new TestEdition("Правильное название");
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(e => e.Name);
        }
    }
}