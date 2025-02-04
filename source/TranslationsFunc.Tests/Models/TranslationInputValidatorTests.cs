// Ignore Spelling: Validator

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
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en"], Word: "word", Meaning: "meaning", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new(SourceLanguage: "", DestinationLanguages: ["ru", "en"], Word: "word", Meaning: "meaning", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Source Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguagesIsNull_ReturnsFalse()
        {
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: null!, Word: "word", Meaning: "meaning", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'DestinationLanguages' must have at least one element and fewer than two.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguagesHasTooManyElements_ReturnsFalse()
        {
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en", "es"], Word: "word", Meaning: "meaning", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'DestinationLanguages' must have at least one element and fewer than two.");
        }

        [TestMethod]
        public void Validate_WhenWordAndMeaningAreEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en"], Word: "", Meaning: "", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Word' or 'Meaning' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenWordIsNotEmptyAndMeaningIsEmpty_ReturnsTrue()
        {
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en"], Word: "word", Meaning: "", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenWordIsEmptyAndMeaningIsNotEmpty_ReturnsTrue()
        {
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en"], Word: "", Meaning: "meaning", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPartOfSpeechIsEmpty_ReturnsTrue()
        {
            // PartOfSpeech is not always present for words in DDO
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en"], Word: "word", Meaning: "meaning", PartOfSpeech: "", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPartOfSpeechIsNotEmpty_ReturnsTrue()
        {
            TranslationInput translationInput = new(SourceLanguage: "da", DestinationLanguages: ["ru", "en"], Word: "word", Meaning: "meaning", PartOfSpeech: "part of speech", []);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }
    }
}
