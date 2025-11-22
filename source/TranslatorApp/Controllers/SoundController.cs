// Ignore Spelling: Validator

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Extensions;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    public class SoundController : ControllerBase
    {
        private readonly ILogger<SoundController> _logger;
        private readonly IValidator<NormalizeSoundRequest> _requestValidatorMock;
        private readonly ISoundService _soundService;
        private readonly IGlobalSettings _globalSettings;

        public SoundController(
            ILogger<SoundController> logger,
            IValidator<NormalizeSoundRequest> normalizeSoundRequestValidator,
            ISoundService soundService,
            IGlobalSettings globalSettings)
        {
            _logger = logger;
            _requestValidatorMock = normalizeSoundRequestValidator;
            _soundService = soundService;
            _globalSettings = globalSettings;
        }

        [HttpPost]
        [Route("api/NormalizeSound")]
        public async Task<IActionResult> NormalizeSoundAsync(
            [FromBody] NormalizeSoundRequest normalizeSoundRequest,
            [FromQuery] string? code = null)
        {
            if (normalizeSoundRequest == null)
            {
                return BadRequest("Input data is null");
            }

            if (!(normalizeSoundRequest.Version == "1"))
            {
                return BadRequest("Only protocol version 1 is supported.");
            }

            if (code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            var validation = await _requestValidatorMock.ValidateAsync(normalizeSoundRequest);
            if (!validation.IsValid)
            {
                string errorMessage = validation.FormatErrorMessage();
                return BadRequest(errorMessage);
            }

            try
            {
                _logger.LogInformation("Received request to normalize sound from URL: {SoundUrl} for word: {Word}", normalizeSoundRequest.SoundUrl, normalizeSoundRequest.Word);

                var ct = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

                // Download the sound file, transcode to mp3 (if necessary), and return the mp3 file stream.
                byte[] fileBytes = await _soundService.DownloadAndNormalizeSoundAsync(normalizeSoundRequest.SoundUrl, normalizeSoundRequest.Word, ct);

                return File(fileBytes, "audio/mpeg", $"{normalizeSoundRequest.Word}.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to normalize the sound.");
                throw;
            }
        }
    }
}
