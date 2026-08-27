using FluentAssertions;
using TranslatorApp.Models;

namespace TranslatorApp.Tests.Models
{
    [TestClass]
    public class SuggestionsRequestValidatorTests
    {
        [TestMethod]
        [DataRow("Danish")]
        [DataRow("spanish")]
        public void Validate_WhenRequestIsValid_ReturnsValid(string destinationLanguage)
        {
            var sut = new SuggestionsRequestValidator();

            var result = sut.Validate(new SuggestionsRequest("привет", destinationLanguage));

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenTextIsEmpty_ReturnsInvalid()
        {
            var sut = new SuggestionsRequestValidator();

            var result = sut.Validate(new SuggestionsRequest(string.Empty, "Danish"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(SuggestionsRequest.Text));
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguageIsUnsupported_ReturnsInvalid()
        {
            var sut = new SuggestionsRequestValidator();

            var result = sut.Validate(new SuggestionsRequest("привет", "English"));

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(SuggestionsRequest.DestinationLanguage));
        }
    }
}
