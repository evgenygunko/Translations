// Ignore Spelling: Downloader ffmpeg

using CopyWords.Parsers.Services;

namespace TranslatorApp.Services
{
    public interface ISoundService
    {
        Task<byte[]> DownloadAndNormalizeSoundAsync(string soundUrl, string word, CancellationToken cancellationToken);
    }

    public class SoundService : ISoundService
    {
        private readonly IFileDownloader _fileDownloader;
        private readonly ILogger<SoundService> _logger;
        private readonly IFileIOService _fileIOService;
        private readonly IFFMpegWrapper _ffmpegWrapper;

        public SoundService(
            IFileDownloader fileDownloader,
            ILogger<SoundService> logger,
            IFileIOService fileIOService,
            IFFMpegWrapper ffmpegWrapper)
        {
            _fileDownloader = fileDownloader;
            _logger = logger;
            _fileIOService = fileIOService;
            _ffmpegWrapper = ffmpegWrapper;
        }

        public async Task<byte[]> DownloadAndNormalizeSoundAsync(string soundUrl, string word, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Downloading sound file from URL: {SoundUrl} for word: {Word}", soundUrl, word);

            byte[] fileBytes = await _fileDownloader.DownloadSoundFileAsync(soundUrl, cancellationToken);

            if (soundUrl.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("Extracting audio from MP4 to MP3 for word: {Word}", word);

                // Use temporary files for more reliable processing in containers
                string tempInputFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
                string tempOutputFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");

                await _fileIOService.WriteAllBytesAsync(tempInputFilePath, fileBytes, cancellationToken);

                try
                {
                    _logger.LogInformation("Wrote input file to {TempPath}, size: {Size} bytes",
                        tempInputFilePath, fileBytes.Length);

                    // Process with FFmpeg using file paths - the pipes return only a few bytes when used in containers
                    await _ffmpegWrapper.ExtractAudioAsync(tempInputFilePath, tempOutputFilePath);

                    byte[] mp3FileBytes = await _fileIOService.ReadAllBytesAsync(tempOutputFilePath, cancellationToken);
                    _logger.LogInformation("Successfully extracted audio to MP3 for word: {Word}. Output size: {Size} bytes",
                        word, mp3FileBytes.Length);

                    return mp3FileBytes;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract audio from MP4 for word: {Word}", word);
                    throw;
                }
            }

            // Return file as is if not MP4
            return fileBytes;
        }
    }
}
