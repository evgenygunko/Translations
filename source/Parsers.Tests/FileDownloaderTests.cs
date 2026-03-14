using System.Net;
using System.Net.Http;
using System.Text;
using CopyWords.Parsers.Exceptions;
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
            var response = CreateResponse(HttpStatusCode.OK, "islygte ");
            using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
            var sut = new FileDownloader(httpClient);

            string? result = await sut.DownloadPageAsync("https://ordnet.dk/ddo/ordbog?query=islygte", Encoding.UTF8, CancellationToken.None);

            result.Should().Be("islygte");
        }

        [TestMethod]
        public async Task DownloadPageAsync_WhenResponseIsNotFound_ReturnsNull()
        {
            var response = CreateResponse(HttpStatusCode.NotFound, "not-found ");
            using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
            var sut = new FileDownloader(httpClient);

            string? result = await sut.DownloadPageAsync("https://ordnet.dk/ddo/ordbog?query=islygte", Encoding.UTF8, CancellationToken.None);

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task DownloadPageAllowNotFoundAsync_WhenResponseIsNotFound_ReturnsContent()
        {
            var response = CreateResponse(HttpStatusCode.NotFound, "islygte ");
            using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
            var sut = new FileDownloader(httpClient);

            string? result = await sut.DownloadPageAllowNotFoundAsync("https://ordnet.dk/ddo/ordbog?query=islygte", Encoding.UTF8, CancellationToken.None);

            result.Should().Be("islygte");
        }

        [TestMethod]
        public async Task DownloadPageAllowNotFoundAsync_WhenResponseIsServerError_Throws()
        {
            var response = CreateResponse(HttpStatusCode.InternalServerError, "error ");
            using var httpClient = new HttpClient(new StubHttpMessageHandler(response));
            var sut = new FileDownloader(httpClient);

            Func<Task> act = async () => await sut.DownloadPageAllowNotFoundAsync("https://ordnet.dk/ddo/ordbog?query=islygte", Encoding.UTF8, CancellationToken.None);

            await act.Should().ThrowAsync<ServerErrorException>();
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

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
    }
}
