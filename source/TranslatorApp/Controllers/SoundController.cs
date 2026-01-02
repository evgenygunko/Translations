// Ignore Spelling: Validator

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    public class SoundController : ControllerBase
    {
        private readonly ILogger<SoundController> _logger;
        private readonly ISoundService _soundService;
        private readonly IGlobalSettings _globalSettings;

        internal TimeSpan DownloadSoundRequestTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public SoundController(
            ILogger<SoundController> logger,
            ISoundService soundService,
            IGlobalSettings globalSettings)
        {
            _logger = logger;
            _soundService = soundService;
            _globalSettings = globalSettings;
        }

        [HttpGet]
        [Route("api/v{version:apiVersion}/[controller]/DownloadSound")]
        [Route("api/DownloadSound")] // Legacy route for backward compatibility
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "soundUrl", "word" })] // cache for 1 week
        public async Task<IActionResult> DownloadSoundAsync(
            [FromQuery] string? soundUrl = null,
            [FromQuery] string? word = null,
            [FromQuery] string? code = null,
            CancellationToken cancellationToken = default)
        {
            if (code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(soundUrl) || string.IsNullOrEmpty(word))
            {
                return BadRequest("soundUrl and word are required");
            }

            using var timeoutCts = new CancellationTokenSource(DownloadSoundRequestTimeout);
            try
            {
                _logger.LogInformation(new EventId((int)TranslatorAppEventId.SoundDownloadRequestReceived),
                    "Received request to download sound from URL: {SoundUrl} for word: {Word}", soundUrl, word);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                byte[] fileBytes = await _soundService.DownloadSoundAsync(soundUrl, word, linkedCts.Token);

                return File(fileBytes, "audio/mpeg", $"{word}.mp3");
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.DownloadingSoundTimedOut),
                    "Sound download timed out after {Timeout} seconds for URL: {SoundUrl}", DownloadSoundRequestTimeout.TotalSeconds, soundUrl);
                return StatusCode(504, "Sound download timed out");
            }
            catch (OperationCanceledException)
            {
                // Client cancelled the request - don't log as error
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId((int)TranslatorAppEventId.ErrorDownloadingSound),
                    ex, "An error occurred while trying to download the sound.");
                throw;
            }
        }
    }
}
