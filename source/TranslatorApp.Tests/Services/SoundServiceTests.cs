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

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundBytes);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] result = await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(soundBytes);

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
        public async Task DownloadAndNormalizeSoundAsync_WithNonMp4Url_ReturnsBytesDirectly()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            byte[] soundBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundBytes);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();
            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] result = await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(soundBytes);

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

            // Verify FFmpeg was not called
            ffmpegWrapperMock.Verify(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [DataRow("https://example.com/sound.mp4")]
        [DataRow("https://example.com/sound.MP4")]
        [DataRow("https://example.com/sound.Mp4")]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_ExtractsAudioSuccessfully(string soundUrl)
        {
            // Arrange
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 }; // Mock MP3 data

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] result = await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(mp3Bytes);

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Extracting audio from MP4 to MP3 for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);

            loggerMock.Verify(x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully extracted audio to MP3 for word: {word}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);

            fileIOServiceMock.Verify(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()), Times.Once);
            fileIOServiceMock.Verify(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            ffmpegWrapperMock.Verify(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            var ffmpegException = new Exception("FFmpeg conversion failed");

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(ffmpegException);

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
            byte[] mp4Bytes = new byte[] { 0x00, 0x00 };

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Conversion failed"));

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

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_WritesInputFileBeforeConversion()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var sut = _fixture.Create<SoundService>();

            // Act
            await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            fileIOServiceMock.Verify(x => x.WriteAllBytesAsync(
                It.Is<string>(path => path.EndsWith(".mp4")),
                mp4Bytes,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_ReadsOutputFileAfterConversion()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var sut = _fixture.Create<SoundService>();

            // Act
            await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            fileIOServiceMock.Verify(x => x.ReadAllBytesAsync(
                It.Is<string>(path => path.EndsWith(".mp3")),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadAndNormalizeSoundAsync_WithMp4Url_PassesCancellationTokenToFileIO()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
            var cancellationToken = new CancellationToken();

            var fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, cancellationToken))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var sut = _fixture.Create<SoundService>();

            // Act
            await sut.DownloadAndNormalizeSoundAsync(soundUrl, word, cancellationToken);

            // Assert
            fileIOServiceMock.Verify(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, cancellationToken), Times.Once);
            fileIOServiceMock.Verify(x => x.ReadAllBytesAsync(It.IsAny<string>(), cancellationToken), Times.Once);
        }

        #endregion
    }
}
