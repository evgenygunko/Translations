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
        private IFixture _fixture = default!;
        private Mock<IGlobalSettings> _globalSettingsMock = default!;

        [TestInitialize]
        public void TestInitialize()
        {
            _fixture = FixtureFactory.CreateWithControllerCustomizations();

            _globalSettingsMock = _fixture.Freeze<Mock<IGlobalSettings>>();
            _globalSettingsMock.Setup(x => x.RequestSecretCode).Returns("test-code");
        }

        #region Tests for DownloadSoundAsync

        [TestMethod]
        public async Task DownloadSoundAsync_WhenSoundUrlIsNull_ReturnsBadRequest()
        {
            // Arrange
            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync(null, "test", "1", "test-code");

            // Assert
            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("soundUrl and word are required");
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WhenWordIsNull_ReturnsBadRequest()
        {
            // Arrange
            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync("https://example.com/sound.mp3", null, "1", "test-code");

            // Assert
            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("soundUrl and word are required");
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WhenSoundUrlIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync(string.Empty, "test", "1", "test-code");

            // Assert
            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("soundUrl and word are required");
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WhenWordIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync("https://example.com/sound.mp3", string.Empty, "1", "test-code");

            // Assert
            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("soundUrl and word are required");
        }

        [TestMethod]
        [DataRow("2")]
        [DataRow("3")]
        public async Task DownloadSoundAsync_WhenUnsupportedProtocolVersion_ReturnsBadRequest(string protocolVersion)
        {
            var sut = _fixture.Create<SoundController>();
            IActionResult actionResult = await sut.DownloadSoundAsync("https://example.com/sound.mp3", "test", protocolVersion, "test-code");

            var result = actionResult as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("Only protocol version 1 is supported.");
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WhenCodeIsInvalid_ReturnsUnauthorized()
        {
            // Arrange
            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync("https://example.com/sound.mp3", "test", "1", "invalid-code");

            // Assert
            var result = actionResult as UnauthorizedResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WhenCodeIsNull_ReturnsUnauthorized()
        {
            // Arrange
            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync("https://example.com/sound.mp3", "test", "1", null);

            // Assert
            var result = actionResult as UnauthorizedResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WhenValidRequest_ReturnsFileResult()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundController>>>();
            var soundServiceMock = _fixture.Freeze<Mock<ISoundService>>();

            var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
            soundServiceMock
                .Setup(x => x.DownloadSoundAsync(soundUrl, word, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedBytes);

            var sut = _fixture.Create<SoundController>();

            // Act
            IActionResult actionResult = await sut.DownloadSoundAsync(soundUrl, word, "1", "test-code");

            // Assert
            var result = actionResult as FileContentResult;
            result.Should().NotBeNull();
            result!.ContentType.Should().Be("audio/mpeg");
            result.FileDownloadName.Should().Be($"{word}.mp3");
            result.FileContents.Should().BeEquivalentTo(expectedBytes);

            soundServiceMock.Verify(x => x.DownloadSoundAsync(soundUrl, word, It.IsAny<CancellationToken>()), Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received request to download sound from URL")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        #endregion
    }
}
