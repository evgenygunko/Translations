// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslatorApp.Models.Input.V1;

namespace TranslatorApp.Tests.Models.Input.V1
{
    [TestClass]
    public class TranslationInputValidatorTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            var translationInput = new TranslationInput(
                Text: "word to look up",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Version: "1");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Validate_WhenTextIsNullOrEmpty_ReturnsFalse(string text)
        {
            var translationInput = new TranslationInput(
                Text: text,
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Version: "2");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_ForUrl_ReturnsFalse()
        {
            string text = _fixture.Create<Uri>().ToString();

            var translationInput = new TranslationInput(
                Text: text,
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Version: "2");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Text' can only contain alphanumeric characters and spaces.");
        }

        [TestMethod]
        public void Validate_ForQuote_ReturnsFalse()
        {
            const string text = "ordbo'g";

            var translationInput = new TranslationInput(
                Text: text,
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Version: "2");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Text' can only contain alphanumeric characters and spaces.");
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            var translationInput = new TranslationInput(
                Text: "word to look up",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: "2");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Source Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguageIsNull_ReturnsFalse()
        {
            var translationInput = new TranslationInput(
                Text: "word to look up",
                SourceLanguage: "da",
                DestinationLanguage: null!,
                Version: "2");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Destination Language' must not be empty.");
        }

        [DataTestMethod]
        [DataRow("1")]
        [DataRow("3")]
        public void Validate_WhenVersionIsNotSupported_ReturnsFalse(string version)
        {
            var translationInput = new TranslationInput(
                Text: "word to look up",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Version: "2");

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Version' must be equal to '1'.");
        }
    }
}
