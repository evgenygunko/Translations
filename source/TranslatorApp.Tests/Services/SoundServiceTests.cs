// Ignore Spelling: Downloader

using AutoFixture;
using CopyWords.Parsers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class SoundServiceTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        #region Tests for DownloadAndNormalizeSoundAsync

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_Should_CallFileDownloader()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            byte[] soundBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var soundStream = new MemoryStream(soundBytes);

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundStream);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            Stream resultStream = await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            resultStream.Should().BeSameAs(soundStream);

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Downloading sound file from URL: {soundUrl} for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
            fileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithNonMp4Url_ReturnsStreamDirectly()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            byte[] soundBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var soundStream = new MemoryStream(soundBytes);

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundStream);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            Stream resultStream = await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            resultStream.Should().BeSameAs(soundStream);
            resultStream.Position.Should().Be(0);

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Downloading sound file from URL: {soundUrl} for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
            fileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl, It.IsAny<CancellationToken>()), Times.Once);

            // Verify no extraction logging occurred
            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Extracting audio")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Never);
        }

        [TestMethod]
        [DataRow("https://example.com/sound.mp4")]
        [DataRow("https://example.com/sound.MP4")]
        [DataRow("https://example.com/sound.Mp4")]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_LogsExtractionStart(string soundUrl)
        {
            // Arrange
            string word = "test";
            var soundStream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x20 }); // Mock MP4 data

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundStream);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act & Assert - Will throw because FFmpeg can't process mock data, but we verify logging occurred
            try
            {
                await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);
            }
            catch
            {
                // Expected to fail with mock data
            }

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Extracting audio from MP4 to MP3 for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WhenFileDownloaderThrowsException_ExceptionPropagatesToCaller()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            var expectedException = new HttpRequestException("Network error");

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                  .WithMessage("Network error");
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_WhenConversionFails_LogsError()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            var soundStream = new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x20 }); // Invalid MP4 data

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundStream);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to extract audio from MP4 for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_WhenConversionFails_ThrowsException()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            var soundStream = new MemoryStream(new byte[] { 0x00, 0x00 }); // Invalid data

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundStream);

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithCancellationToken_PassesToFileDownloader()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            var cancellationToken = new CancellationToken(canceled: true);
            var soundStream = new MemoryStream(new byte[] { 0x01, 0x02 });

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), cancellationToken))
                .ThrowsAsync(new OperationCanceledException());

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, cancellationToken);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            fileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl, cancellationToken), Times.Once);
        }

        #endregion
    }
}
