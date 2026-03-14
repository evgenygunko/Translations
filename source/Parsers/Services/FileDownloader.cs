// Ignore Spelling: Downloader

using System.Net;
using System.Text;
using CopyWords.Parsers.Exceptions;

namespace CopyWords.Parsers.Services
{
    public interface IFileDownloader
    {
        Task<string?> DownloadPageAsync(string url, Encoding encoding, CancellationToken cancellationToken);

        Task<string?> DownloadPageAllowNotFoundAsync(string url, Encoding encoding, CancellationToken cancellationToken);

        Task<byte[]> DownloadSoundFileAsync(string url, CancellationToken cancellationToken);
    }

    public class FileDownloader : IFileDownloader
    {
        private readonly HttpClient _httpClient;

        public FileDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");
        }

        public async Task<string?> DownloadPageAsync(string url, Encoding encoding, CancellationToken cancellationToken)
        {
            return await DownloadPageInternalAsync(url, encoding, returnContentOnNotFound: false, cancellationToken);
        }

        public async Task<string?> DownloadPageAllowNotFoundAsync(string url, Encoding encoding, CancellationToken cancellationToken)
        {
            return await DownloadPageInternalAsync(url, encoding, returnContentOnNotFound: true, cancellationToken);
        }

        public async Task<byte[]> DownloadSoundFileAsync(string url, CancellationToken cancellationToken)
        {
            byte[]? content = null;
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ServerErrorException($"Server returned error code '{response.StatusCode}' when requesting URL '{url}'.");
            }

            content = await response.Content.ReadAsByteArrayAsync();

            if (content == null)
            {
                throw new ServerErrorException($"Downloaded sound file from URL '{url}' is null.");
            }

            return content;
        }

        private async Task<string?> DownloadPageInternalAsync(string url, Encoding encoding, bool returnContentOnNotFound, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode || (returnContentOnNotFound && response.StatusCode == HttpStatusCode.NotFound))
            {
                byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                return encoding.GetString(bytes, 0, bytes.Length - 1);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new ServerErrorException(
                $"Server returned error code '{response.StatusCode}' when requesting URL '{url}'.",
                response.StatusCode,
                url);
        }
    }
}
