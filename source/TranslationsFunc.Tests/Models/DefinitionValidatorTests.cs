// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;

namespace TranslationFunc.Tests.Models
{
    [TestClass]
    public class DefinitionValidatorTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            DefinitionInput definition = new DefinitionInput(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: new HeadwordInput("word", "Meaning", Examples: []),
                Contexts: []
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            DefinitionInput definition = new DefinitionInput(
                id: 0,
                PartOfSpeech: "PartOfSpeech",
                Headword: new HeadwordInput("word", "Meaning", Examples: []),
                Contexts: []
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorMessage.Should().Be("'id' must be greater than '0'.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordIsNull_ReturnsFalse()
        {
            DefinitionInput definition = new DefinitionInput(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: null!,
                Contexts: []
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("Each definition must have a Headword.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordTextIsEmpty_ReturnsFalse()
        {
            DefinitionInput definition = new DefinitionInput(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: new HeadwordInput("", "Meaning", Examples: []),
                Contexts: []
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Headword Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenContextsAreNull_ReturnsFalse()
        {
            DefinitionInput definition = new DefinitionInput(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: new HeadwordInput("word", "Meaning", Examples: []),
                Contexts: null!
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
        }
    }
}
