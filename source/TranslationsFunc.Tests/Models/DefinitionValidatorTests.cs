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
            Definition2 definition = new Definition2(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: new Headword2("word", "Meaning", Examples: []),
                Contexts: []
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            Definition2 definition = new Definition2(
                id: 0,
                PartOfSpeech: "PartOfSpeech",
                Headword: new Headword2("word", "Meaning", Examples: []),
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
            Definition2 definition = new Definition2(
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
            Definition2 definition = new Definition2(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: new Headword2("", "Meaning", Examples: []),
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
            Definition2 definition = new Definition2(
                id: 1,
                PartOfSpeech: "PartOfSpeech",
                Headword: new Headword2("word", "Meaning", Examples: []),
                Contexts: null!
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
        }
    }
}
