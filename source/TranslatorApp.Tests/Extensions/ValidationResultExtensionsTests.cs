// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslatorApp.Extensions;

namespace TranslatorApp.Tests.Extensions
{
    [TestClass]
    public class ValidationResultExtensionsTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        [TestMethod]
        public void FormatErrorMessage_WhenOneError_AddsFullStopAtTheEnd()
        {
            // Arrange
            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("property1", "property1 cannot be null"));

            // Act
            string result = validationResult.FormatErrorMessage();

            // Assert
            result.Should().Be("Error: property1 cannot be null.");
        }

        [TestMethod]
        public void FormatErrorMessage_WhenTwoErrors_AddsFullStopAtTheEnd()
        {
            // Arrange
            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("property1", "property1 cannot be null"));
            validationResult.Errors.Add(new ValidationFailure("property2", "property2 cannot be null"));

            // Act
            string result = validationResult.FormatErrorMessage();

            // Assert
            result.Should().Be("Error: property1 cannot be null, property2 cannot be null.");
        }
    }
}
