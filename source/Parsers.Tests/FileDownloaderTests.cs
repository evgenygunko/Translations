// Ignore Spelling: Downloader

using System.Net;
using System.Text;
using CopyWords.Parsers.Services;
using FluentAssertions;

namespace CopyWords.Parsers.Tests
{
    [TestClass]
    public class FileDownloaderTests
    {
        [TestMethod]
        public async Task DownloadPageAsync_WhenResponseIsSuccess_ReturnsDecodedContent()
        {
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, "islygte "));
            using var httpClient = new HttpClient(handler);
            var sut = CreateSut(httpClient);

            string? result = await sut.DownloadPageAsync("https://gammel.ordnet.dk/ddo/ordbog?query=islygte", Encoding.UTF8, CancellationToken.None);

            result.Should().Be("islygte");
        }

        [TestMethod]
        public async Task DownloadPageAsync_WhenResponseIsNotFound_ReturnsNull()
        {
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.NotFound, "not-found "));
            using var httpClient = new HttpClient(handler);
            var sut = CreateSut(httpClient);

            string? result = await sut.DownloadPageAsync("https://gammel.ordnet.dk/ddo/ordbog?query=islygte", Encoding.UTF8, CancellationToken.None);

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task DownloadPageAsync_WhenRequestTargetsDdo_AddsBrowserNavigationHeaders()
        {
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, "islygte "));
            using var httpClient = new HttpClient(handler);
            var sut = CreateSut(httpClient);
            const string url = "https://gammel.ordnet.dk/ddo/ordbog?query=bedrift";

            await sut.DownloadPageAsync(url, Encoding.UTF8, CancellationToken.None);

            handler.LastRequest.Should().NotBeNull();
            HttpRequestMessage request = handler.LastRequest!;
            request.Headers.Referrer.Should().BeNull();
            request.Headers.UserAgent.ToString().Should().Be("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/151.0.0.0 Safari/537.36");
            request.Headers.Accept.ToString().Should().Be("text/html, application/xhtml+xml, application/xml; q=0.9, image/avif, image/webp, image/apng, */*; q=0.8, application/signed-exchange; v=b3; q=0.7");
            request.Headers.AcceptLanguage.ToString().Should().Be("en, ru; q=0.9, da; q=0.8, es; q=0.7");
            request.Headers.GetValues("Upgrade-Insecure-Requests").Should().ContainSingle().Which.Should().Be("1");
            request.Headers.GetValues("Sec-Fetch-Dest").Should().ContainSingle().Which.Should().Be("document");
            request.Headers.GetValues("Sec-Fetch-Mode").Should().ContainSingle().Which.Should().Be("navigate");
            request.Headers.GetValues("Sec-Fetch-Site").Should().ContainSingle().Which.Should().Be("none");
            request.Headers.GetValues("Sec-Fetch-User").Should().ContainSingle().Which.Should().Be("?1");
            request.Headers.GetValues("sec-ch-ua").Should().ContainSingle().Which.Should().Be("\"Not=A?Brand\";v=\"99\", \"Google Chrome\";v=\"151\", \"Chromium\";v=\"151\"");
            request.Headers.GetValues("sec-ch-ua-mobile").Should().ContainSingle().Which.Should().Be("?0");
            request.Headers.GetValues("sec-ch-ua-platform").Should().ContainSingle().Which.Should().Be("\"Windows\"");
        }

        [TestMethod]
        public async Task DownloadPageAsync_WhenRequestIsNotDdo_DoesNotAddDdoOnlyHeaders()
        {
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, "page "));
            using var httpClient = new HttpClient(handler);
            var sut = CreateSut(httpClient);

            await sut.DownloadPageAsync("https://example.com/page", Encoding.UTF8, CancellationToken.None);

            handler.LastRequest.Should().NotBeNull();
            HttpRequestMessage request = handler.LastRequest!;
            request.Headers.Referrer.Should().BeNull();
            request.Headers.Contains("Sec-Fetch-Dest").Should().BeFalse();
            request.Headers.Contains("Sec-Fetch-Mode").Should().BeFalse();
            request.Headers.Contains("Sec-Fetch-Site").Should().BeFalse();
            request.Headers.Contains("Sec-Fetch-User").Should().BeFalse();
            request.Headers.Contains("sec-ch-ua").Should().BeFalse();
            request.Headers.Contains("sec-ch-ua-mobile").Should().BeFalse();
            request.Headers.Contains("sec-ch-ua-platform").Should().BeFalse();
        }

        private static FileDownloader CreateSut(HttpClient httpClient)
        {
            return new FileDownloader(httpClient);
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            return new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(bytes)
            };
        }

        private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response = response;

            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_response);
            }
        }
    }
}
