// Ignore Spelling: Downloader

using AutoFixture;
using CopyWords.Parsers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Exceptions;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class SoundServiceTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        #region Tests for SaveSoundAsync

        [TestMethod]
        public async Task SaveSoundAsync_Should_CallDownloadSoundFileAsync()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            byte[] soundBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>())).ReturnsAsync(soundBytes);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] soundFile = await sut.SaveSoundAsync(soundUrl, word);

            // Assert
            soundFile.Should().BeEquivalentTo(soundBytes);

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Downloading sound file from URL: {soundUrl} for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
            fileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl), Times.Once);
        }

        [TestMethod]
        public async Task SaveSoundAsync_WhenFileDownloaderReturnsNull_ThrowsCannotDownloadSoundFileException()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>())).ReturnsAsync((byte[]?)null);

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.SaveSoundAsync(soundUrl, word);

            // Assert
            await act.Should().ThrowAsync<CannotDownloadSoundFileException>();
        }

        [TestMethod]
        public async Task SaveSoundAsync_WhenFileDownloaderThrowsException_ExceptionPropagatesToCaller()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            var expectedException = new HttpRequestException("Network error");

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>())).ThrowsAsync(expectedException);

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.SaveSoundAsync(soundUrl, word);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                  .WithMessage("Network error");
        }

        #endregion
    }
}
