﻿// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;

namespace TranslationFunc.Tests.Models
{
    [TestClass]
    public class TranslationInputValidatorTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "",
                DestinationLanguages: ["ru", "en"],
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Source Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguagesIsNull_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: null!,
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'DestinationLanguages' must have at least one element and fewer than two.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguagesHasTooManyElements_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en", "es"],
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'DestinationLanguages' must have at least one element and fewer than two.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordIsNull_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: default!,
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Headword' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordTextIsEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: new Headword("", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Headword Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenMeaningsAreEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
        }

        [DataTestMethod]
        [DataRow("0")]
        [DataRow("2")]
        public void Validate_WhenVersionDoesNotEqual1_ReturnsFalse(string version)
        {
            TranslationInput translationInput = new TranslationInput(
                Version: version,
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: new Headword("word", "Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
        }
    }
}
