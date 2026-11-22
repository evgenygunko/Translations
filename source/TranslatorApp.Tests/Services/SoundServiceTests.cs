// Ignore Spelling: Downloader

using AutoFixture;
using FluentAssertions;
using FFMpegCore;
using System.Reflection;
using TranslatorApp.Models;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class SoundServiceTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        // FFMpegResult exposes only an internal constructor.
        private static FFMpegResult CreateFFMpegResult(int exitCode, bool cancelled = false)
        {
            return (FFMpegResult)Activator.CreateInstance(
                typeof(FFMpegResult),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { exitCode, new[] { "FFmpeg diagnostic" }, cancelled },
                culture: null)!;
        }

        #region Tests for DownloadSoundAsync

        [TestMethod]
        public async Task DownloadSoundAsync_Should_CallSoundFileDownloader()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            byte[] soundBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundBytes);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] result = await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

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
            soundFileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WithNonMp4Url_ReturnsBytesDirectly()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            byte[] soundBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(soundBytes);

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();
            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] result = await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

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
            soundFileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl, It.IsAny<CancellationToken>()), Times.Once);

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
        public async Task DownloadSoundAsync_WithMp4Url_ExtractsAudioSuccessfully(string soundUrl)
        {
            // Arrange
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 }; // Mock MP3 data

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(CreateFFMpegResult(0));

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();

            var sut = _fixture.Create<SoundService>();

            // Act
            byte[] result = await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

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
        public async Task DownloadSoundAsync_WhenSoundFileDownloaderThrowsException_ExceptionPropagatesToCaller()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            var expectedException = new HttpRequestException("Network error");

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                  .WithMessage("Network error");
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WithMp4Url_WhenConversionFails_LogsError()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            var ffmpegException = new Exception("FFmpeg conversion failed");

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            Func<Task> act = async () => await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

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
        public async Task DownloadSoundAsync_WithMp4Url_WhenConversionFails_ThrowsException()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00 };

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Conversion failed"));

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WithCancellationToken_PassesToSoundFileDownloader()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp3";
            string word = "test";
            var cancellationToken = new CancellationToken(canceled: true);

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), cancellationToken))
                .ThrowsAsync(new OperationCanceledException());

            var sut = _fixture.Create<SoundService>();

            // Act
            Func<Task> act = async () => await sut.DownloadSoundAsync(soundUrl, word, cancellationToken);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
            soundFileDownloaderMock.Verify(x => x.DownloadSoundFileAsync(soundUrl, cancellationToken), Times.Once);
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WithMp4Url_WritesInputFileBeforeConversion()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(CreateFFMpegResult(0));

            var sut = _fixture.Create<SoundService>();

            // Act
            await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            fileIOServiceMock.Verify(x => x.WriteAllBytesAsync(
                It.Is<string>(path => path.EndsWith(".mp4")),
                mp4Bytes,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WithMp4Url_ReadsOutputFileAfterConversion()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(CreateFFMpegResult(0));

            var sut = _fixture.Create<SoundService>();

            // Act
            await sut.DownloadSoundAsync(soundUrl, word, CancellationToken.None);

            // Assert
            fileIOServiceMock.Verify(x => x.ReadAllBytesAsync(
                It.Is<string>(path => path.EndsWith(".mp3")),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadSoundAsync_WithMp4Url_PassesCancellationTokenToFileIO()
        {
            // Arrange
            string soundUrl = "https://example.com/sound.mp4";
            string word = "test";
            byte[] mp4Bytes = new byte[] { 0x00, 0x00, 0x00, 0x20 };
            byte[] mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
            var cancellationToken = new CancellationToken();

            var soundFileDownloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            soundFileDownloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(mp4Bytes);

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, cancellationToken))
                .Returns(Task.CompletedTask);
            fileIOServiceMock.Setup(x => x.ReadAllBytesAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(mp3Bytes);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(CreateFFMpegResult(0));

            var sut = _fixture.Create<SoundService>();

            // Act
            await sut.DownloadSoundAsync(soundUrl, word, cancellationToken);

            // Assert
            fileIOServiceMock.Verify(x => x.WriteAllBytesAsync(It.IsAny<string>(), mp4Bytes, cancellationToken), Times.Once);
            fileIOServiceMock.Verify(x => x.ReadAllBytesAsync(It.IsAny<string>(), cancellationToken), Times.Once);
        }

        [TestMethod]
        [DataRow(1, false)]
        [DataRow(0, true)]
        public async Task DownloadSoundAsync_WhenExtractionResultIsUnsuccessful_LogsAndThrows(int exitCode, bool cancelled)
        {
            var downloaderMock = _fixture.Freeze<Mock<ISoundFileDownloader>>();
            downloaderMock.Setup(x => x.DownloadSoundFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 0x01 });

            var fileIOServiceMock = _fixture.Freeze<Mock<IFileIOService>>();
            fileIOServiceMock.Setup(x => x.WriteAllBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var ffmpegWrapperMock = _fixture.Freeze<Mock<IFFMpegWrapper>>();
            ffmpegWrapperMock.Setup(x => x.ExtractAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(CreateFFMpegResult(exitCode, cancelled));

            var loggerMock = _fixture.Freeze<Mock<ILogger<SoundService>>>();
            var sut = _fixture.Create<SoundService>();

            Func<Task> act = async () => await sut.DownloadSoundAsync("https://example.com/sound.mp4", "test", CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"Audio extraction failed with exit code {exitCode}. Cancelled: {cancelled}. FFmpeg diagnostic");

            loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.Is<EventId>(id => id.Id == (int)TranslatorAppEventId.AudioExtractionFailed),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Failed to extract audio from MP4 for word: test")),
                It.Is<Exception>(ex => ex is InvalidOperationException && ex.Message.Contains("FFmpeg diagnostic")),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!), Times.Once);

            fileIOServiceMock.Verify(x => x.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}
