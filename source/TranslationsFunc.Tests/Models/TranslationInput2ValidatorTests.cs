﻿// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;

namespace TranslationFunc.Tests.Models
{
    [TestClass]
    public class TranslationInput2ValidatorTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word",
                Headword: new Headword("Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "1",
                SourceLanguage: "",
                DestinationLanguages: ["ru", "en"],
                Word: "word",
                Headword: new Headword("Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Source Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguagesIsNull_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: null!,
                Word: "word",
                Headword: new Headword("Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'DestinationLanguages' must have at least one element and fewer than two.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguagesHasTooManyElements_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en", "es"],
                Word: "word",
                Headword: new Headword("Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'DestinationLanguages' must have at least one element and fewer than two.");
        }

        [TestMethod]
        public void Validate_WhenWordIsEmpty_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "",
                Headword: new Headword("Meaning", "PartOfSpeech", Examples: []),
                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Word' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenMeaningsAreEmpty_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word",
                Headword: new Headword("Meaning", "PartOfSpeech", Examples: []),
                Meanings: []);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
        }
    }
}
