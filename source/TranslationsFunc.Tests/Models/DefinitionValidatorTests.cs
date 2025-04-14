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
            Definition definition = new Definition(
                id: 1,
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            Definition definition = new Definition(
                id: 0,
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].ErrorMessage.Should().Be("Each definition id must be an integer greater than 0.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordIsNull_ReturnsFalse()
        {
            Definition definition = new Definition(
                id: 1,
                Headword: null!,
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]
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
            Definition definition = new Definition(
                id: 1,
                Headword: new Headword("", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Headword Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenMeaningsAreNull_ReturnsFalse()
        {
            Definition definition = new Definition(
                id: 1,
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: null!
            );

            var sut = _fixture.Create<DefinitionValidator>();
            ValidationResult result = sut.Validate(definition);

            result.IsValid.Should().BeFalse();
        }
    }
}
