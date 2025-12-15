// Ignore Spelling: Validator

using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    public class SoundController : ControllerBase
    {
        private readonly ILogger<SoundController> _logger;
        private readonly ISoundService _soundService;
        private readonly IGlobalSettings _globalSettings;

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
        [Route("api/DownloadSound")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "soundUrl", "word" })] // cache for 1 week
        public async Task<IActionResult> DownloadSoundAsync(
            [FromQuery] string? soundUrl = null,
            [FromQuery] string? word = null,
            [FromQuery] string? version = "1",
            [FromQuery] string? code = null)
        {
            if (code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(soundUrl) || string.IsNullOrEmpty(word))
            {
                return BadRequest("soundUrl and word are required");
            }

            if (!(version == "1"))
            {
                return BadRequest("Only protocol version 1 is supported.");
            }

            try
            {
                _logger.LogInformation(new EventId((int)TranslatorAppEventId.SoundDownloadRequestReceived),
                    "Received request to download sound from URL: {SoundUrl} for word: {Word}", soundUrl, word);

                var ct = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

                // Download the sound file, transcode to mp3 (if necessary), and return the mp3 file stream.
                byte[] fileBytes = await _soundService.DownloadSoundAsync(soundUrl, word, ct);

                return File(fileBytes, "audio/mpeg", $"{word}.mp3");
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
