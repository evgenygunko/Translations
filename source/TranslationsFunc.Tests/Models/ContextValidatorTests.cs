// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;

namespace TranslationFunc.Tests.Models
{
    [TestClass]
    public class ContextValidatorTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            Context context = new Context(id: 1, ContextEN: "context 1", Meanings: []);

            var sut = _fixture.Create<ContextValidator>();
            ValidationResult result = sut.Validate(context);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            Context context = new Context(id: 0, ContextEN: "context 1", Meanings: []);

            var sut = _fixture.Create<ContextValidator>();
            ValidationResult result = sut.Validate(context);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorMessage.Should().Be("'id' must be greater than '0'.");
        }

        [TestMethod]
        public void Validate_WhenMeaningsAreNull_ReturnsFalse()
        {
            Context context = new Context(id: 0, ContextEN: "context 1", Meanings: null!);

            var sut = _fixture.Create<ContextValidator>();
            ValidationResult result = sut.Validate(context);

            result.IsValid.Should().BeFalse();
        }
    }
}
