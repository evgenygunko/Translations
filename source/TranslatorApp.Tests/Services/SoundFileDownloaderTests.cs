using System.Net;
using FluentAssertions;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class SoundFileDownloaderTests
    {
        [TestMethod]
        public async Task DownloadSoundFileAsync_WhenResponseIsSuccessful_ReturnsBytes()
        {
            byte[] expected = [0x01, 0x02, 0x03];
            using var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expected)
            };
            using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
            var sut = new SoundFileDownloader(httpClient);

            byte[] result = await sut.DownloadSoundFileAsync("https://example.com/sound.mp3", CancellationToken.None);

            result.Should().Equal(expected);
        }

        [TestMethod]
        public async Task DownloadSoundFileAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
            var sut = new SoundFileDownloader(httpClient);

            Func<Task> action = () => sut.DownloadSoundFileAsync("https://example.com/missing.mp3", CancellationToken.None);

            var assertion = await action.Should().ThrowAsync<HttpRequestException>().WithMessage("*404*");
            assertion.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DownloadSoundFileAsync_WhenCancelled_PropagatesCancellation()
        {
            var handler = new CancellableHttpMessageHandler();
            using var httpClient = new HttpClient(handler);
            var sut = new SoundFileDownloader(httpClient);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(10));

            Func<Task> action = () => sut.DownloadSoundFileAsync("https://example.com/sound.mp3", cancellationTokenSource.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
            handler.CancellationToken.CanBeCanceled.Should().BeTrue();
            handler.CancellationToken.IsCancellationRequested.Should().BeTrue();
        }

        private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(response);
            }
        }

        private sealed class CancellableHttpMessageHandler : HttpMessageHandler
        {
            public CancellationToken CancellationToken { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The cancellation token was not propagated.");
            }
        }
    }
}
