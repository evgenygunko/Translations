// Ignore Spelling: Validator

using System.Net;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Controllers;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Controllers
{
    [TestClass]
    public class SoundControllerTests
    {
        private readonly IFixture _fixture = FixtureFactory.CreateWithControllerCustomizations();

        #region Tests for NormalizeSoundAsync

        [TestMethod]
        public async Task NormalizeSoundAsync_WhenInputDataIsNull_ReturnsBadRequest()
        {
            // Arrange
            NormalizeSoundRequest? normalizeSoundRequest = null;

            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.NormalizeSoundAsync(normalizeSoundRequest!);

            // Assert
            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("Input data is null");
        }

        [TestMethod]
        public async Task NormalizeSoundAsync_WhenValidationHasMultipleErrors_ReturnsBadRequest()
        {
            // Arrange
            var normalizeSoundRequest = new NormalizeSoundRequest(SoundUrl: string.Empty, Word: string.Empty);

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SoundUrl", "'SoundUrl' must not be empty"));
            validationResult.Errors.Add(new ValidationFailure("Word", "'Word' must not be empty"));

            var normalizeSoundRequestValidatorMock = _fixture.Freeze<Mock<IValidator<NormalizeSoundRequest>>>();
            normalizeSoundRequestValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<NormalizeSoundRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(validationResult);

            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.NormalizeSoundAsync(normalizeSoundRequest);

            // Assert
            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("Error: 'SoundUrl' must not be empty, 'Word' must not be empty.");
        }

        [TestMethod]
        public async Task NormalizeSoundAsync_WhenValidRequest_ReturnsFileResult()
        {
            // Arrange
            var normalizeSoundRequest = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: "test");

            var normalizeSoundRequestValidatorMock = _fixture.Freeze<Mock<IValidator<NormalizeSoundRequest>>>();
            normalizeSoundRequestValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<NormalizeSoundRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundController>>>();
            var soundServiceMock = _fixture.Freeze<Mock<ISoundService>>();

            var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
            soundServiceMock
                .Setup(x => x.SaveSoundAsync(normalizeSoundRequest.SoundUrl, normalizeSoundRequest.Word))
                .ReturnsAsync(expectedBytes);

            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.NormalizeSoundAsync(normalizeSoundRequest);

            // Assert
            var result = actionResult as FileContentResult;
            result.Should().NotBeNull();
            result!.ContentType.Should().Be("audio/mpeg");
            result.FileDownloadName.Should().Be($"{normalizeSoundRequest.Word}.mp3");
            result.FileContents.Should().BeEquivalentTo(expectedBytes);

            // Assert
            soundServiceMock.Verify(x => x.SaveSoundAsync(normalizeSoundRequest.SoundUrl, normalizeSoundRequest.Word), Times.Once);

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received request to normalize sound from URL")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
              Times.Once);
        }

        #endregion
    }
}
