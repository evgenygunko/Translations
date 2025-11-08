// Ignore Spelling: Downloader

using CopyWords.Parsers.Services;

namespace TranslatorApp.Services
{
    public interface ISoundService
    {
        Task<byte[]> SaveSoundAsync(string soundUrl, string word);
    }

    public class SoundService : ISoundService
    {
        private readonly IFileDownloader _fileDownloader;
        private readonly ILogger<SoundService> _logger;

        public SoundService(
            IFileDownloader fileDownloader,
            ILogger<SoundService> logger)
        {
            _fileDownloader = fileDownloader;
            _logger = logger;
        }

        public async Task<byte[]> SaveSoundAsync(string soundUrl, string word)
        {
            _logger.LogInformation("Downloading sound file from URL: {SoundUrl} for word: {Word}", soundUrl, word);

            var fileBytes = await _fileDownloader.DownloadSoundFileAsync(soundUrl);
            if (fileBytes != null)
            {
                // todo: to implement normalization and saving to disk
            }

            throw new NotImplementedException("SaveSoundAsync implementation is pending");
        }
    }
}
