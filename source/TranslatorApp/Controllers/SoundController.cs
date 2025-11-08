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
        private readonly IValidator<NormalizeSoundRequest> _normalizeSoundRequestValidator;
        private readonly ISoundService _soundService;

        public SoundController(
            ILogger<SoundController> logger,
            IValidator<NormalizeSoundRequest> normalizeSoundRequestValidator,
            ISoundService soundService)
        {
            _logger = logger;
            _normalizeSoundRequestValidator = normalizeSoundRequestValidator;
            _soundService = soundService;
        }

        [HttpPost]
        [Route("api/NormalizeSound")]
        public async Task<IActionResult> NormalizeSoundAsync([FromBody] NormalizeSoundRequest request)
        {
            if (request == null)
            {
                return BadRequest("Input data is null");
            }

            var validation = await _normalizeSoundRequestValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                string errorMessage = validation.FormatErrorMessage();
                return BadRequest(errorMessage);
            }

            try
            {
                _logger.LogInformation("Received request to normalize sound from URL: {SoundUrl} for word: {Word}", request.SoundUrl, request.Word);

                // This will download the sound file, normalize it, and return the normalized audio
                byte[] soundFile = await _soundService.SaveSoundAsync(request.SoundUrl, request.Word);

                return File(soundFile, "audio/mpeg", $"{request.Word}.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to normalize the sound.");
                throw;
            }
        }
    }
}
