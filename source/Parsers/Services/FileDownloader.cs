// Ignore Spelling: Downloader

using System.Text;
using CopyWords.Parsers.Exceptions;

namespace CopyWords.Parsers.Services
{
    public interface IFileDownloader
    {
        Task<string?> DownloadPageAsync(string url, Encoding encoding, CancellationToken cancellationToken);

        Task<Stream> DownloadSoundFileAsync(string url, CancellationToken cancellationToken);
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
            string? content = null;
            HttpResponseMessage response;

            response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                content = encoding.GetString(bytes, 0, bytes.Length - 1);
            }
            else
            {
                if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw new ServerErrorException($"Server returned error code '{response.StatusCode}' when requesting URL '{url}'.");
                }
            }

            return content;
        }

        public async Task<Stream> DownloadSoundFileAsync(string url, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ServerErrorException($"Server returned error code '{response.StatusCode}' when requesting URL '{url}'.");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}
