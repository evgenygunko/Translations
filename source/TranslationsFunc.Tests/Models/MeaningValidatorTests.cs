// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;

namespace TranslationFunc.Tests.Models
{
    [TestClass]
    public class MeaningValidatorTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            MeaningInput meaning = new MeaningInput(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"]);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            MeaningInput meaning = new MeaningInput(id: 0, Text: "meaning 1", Examples: ["Meaning 1, example 1"]);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorMessage.Should().Be("'id' must be greater than '0'.");
        }

        [TestMethod]
        public void Validate_WhenTextIsNull_ReturnsFalse()
        {
            MeaningInput meaning = new MeaningInput(id: 1, Text: null!, Examples: ["Meaning 1, example 1"]);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenExamplesAreNull_ReturnsFalse()
        {
            MeaningInput meaning = new MeaningInput(id: 1, Text: "meaning 1", Examples: null!);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeFalse();
        }
    }
}
