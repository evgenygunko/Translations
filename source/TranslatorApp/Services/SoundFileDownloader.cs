namespace TranslatorApp.Services
{
    public interface ISoundFileDownloader
    {
        Task<byte[]> DownloadSoundFileAsync(string url, CancellationToken cancellationToken);
    }

    public class SoundFileDownloader : ISoundFileDownloader
    {
        private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/151.0.0.0 Safari/537.36";
        private const string BrowserAcceptLanguageHeader = "en,ru;q=0.9,da;q=0.8,es;q=0.7";

        private readonly HttpClient _httpClient;

        public SoundFileDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(BrowserAcceptLanguageHeader);
        }

        public async Task<byte[]> DownloadSoundFileAsync(string url, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
    }
}
