// Ignore Spelling: Downloader

using CopyWords.Parsers.Services;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace TranslatorApp.Services
{
    public interface ISoundService
    {
        Task<Stream> DownloadAndNormalizeSoundAsync(string soundUrl, string word, CancellationToken cancellationToken);
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

        public async Task<Stream> DownloadAndNormalizeSoundAsync(string soundUrl, string word, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Downloading sound file from URL: {SoundUrl} for word: {Word}", soundUrl, word);

            Stream inputStream = await _fileDownloader.DownloadSoundFileAsync(soundUrl, cancellationToken);

            if (soundUrl.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("Extracting audio from MP4 to MP3 for word: {Word}", word);

                var outputStream = new MemoryStream();
                var success = false;

                try
                {
                    await FFMpegArguments
                        .FromPipeInput(new StreamPipeSource(inputStream))
                        .OutputToPipe(new StreamPipeSink(outputStream), options => options
                            .WithAudioCodec("libmp3lame")
                            .WithAudioBitrate(128)
                            .ForceFormat("mp3"))
                        .ProcessAsynchronously();

                    outputStream.Position = 0;
                    _logger.LogInformation("Successfully extracted audio to MP3 for word: {Word}", word);
                    success = true;
                    return outputStream;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract audio from MP4 for word: {Word}", word);
                    throw;
                }
                finally
                {
                    // Always dispose inputStream (no longer needed after FFmpeg processing)
                    await inputStream.DisposeAsync();

                    // Only dispose outputStream if conversion failed
                    if (!success)
                    {
                        await outputStream.DisposeAsync();
                    }
                }
            }

            // Return input stream directly - caller takes ownership
            return inputStream;
        }
    }
}
