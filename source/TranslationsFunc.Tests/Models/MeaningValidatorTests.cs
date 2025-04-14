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
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            Meaning meaning = new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"]);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            Meaning meaning = new Meaning(id: 0, Text: "meaning 1", Examples: ["Meaning 1, example 1"]);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorMessage.Should().Be("Each meaning id must be an integer greater than 0.");
        }

        [TestMethod]
        public void Validate_WhenTextIsNull_ReturnsFalse()
        {
            Meaning meaning = new Meaning(id: 1, Text: null!, Examples: ["Meaning 1, example 1"]);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenExamplesAreNull_ReturnsFalse()
        {
            Meaning meaning = new Meaning(id: 1, Text: "meaning 1", Examples: null!);

            var sut = _fixture.Create<MeaningValidator>();
            ValidationResult result = sut.Validate(meaning);

            result.IsValid.Should().BeFalse();
        }
    }
}
