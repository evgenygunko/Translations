// Ignore Spelling: Validator App

using AutoFixture;
using CopyWords.Parsers.Models;
using FluentAssertions;
using FluentValidation.Results;
using TranslatorApp.Models;

namespace TranslatorApp.Tests.Models
{
    [TestClass]
    public class LookUpWordRequestValidatorTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            var translationInput = new LookUpWordRequest(
                Text: "word to look up",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Validate_WhenTextIsNullOrEmpty_ReturnsFalse(string text)
        {
            var translationInput = new LookUpWordRequest(
                Text: text,
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_ForQuote_ReturnsTrue()
        {
            const string text = "ordbo'g";

            var translationInput = new LookUpWordRequest(
                Text: text,
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            var translationInput = new LookUpWordRequest(
                Text: "word to look up",
                SourceLanguage: "",
                DestinationLanguage: "ru");

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'SourceLanguage' must be one of the following: Danish, Spanish");
        }

        [TestMethod]
        [DataRow(SourceLanguage.Danish)]
        [DataRow(SourceLanguage.Spanish)]
        public void Validate_WhenSourceLanguageIsValidEnumValue_ReturnsTrue(SourceLanguage sourceLanguage)
        {
            var translationInput = new LookUpWordRequest(
                Text: "word to look up",
                SourceLanguage: sourceLanguage.ToString(),
                DestinationLanguage: "ru");

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("English")]
        [DataRow("da")]
        [DataRow("es")]
        [DataRow("123")]
        public void Validate_WhenSourceLanguageIsNotValidEnumValue_ReturnsFalse(string sourceLanguage)
        {
            var translationInput = new LookUpWordRequest(
                Text: "word to look up",
                SourceLanguage: sourceLanguage,
                DestinationLanguage: "ru");

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "'SourceLanguage' must be one of the following: Danish, Spanish");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguageIsNull_ReturnsFalse()
        {
            var translationInput = new LookUpWordRequest(
                Text: "word to look up",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: null!);

            var sut = _fixture.Create<LookUpWordRequestValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Destination Language' must not be empty.");
        }
    }
}
