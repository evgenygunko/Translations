// Ignore Spelling: Validator

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Extensions;
using TranslatorApp.Models;

namespace TranslatorApp.Controllers
{
    [ApiController]
    public class SoundController : ControllerBase
    {
        private readonly ILogger<SoundController> _logger;
        private readonly IValidator<NormalizeSoundRequest> _normalizeSoundRequestValidator;

        public SoundController(
            ILogger<SoundController> logger,
            IValidator<NormalizeSoundRequest> normalizeSoundRequestValidator)
        {
            _logger = logger;
            _normalizeSoundRequestValidator = normalizeSoundRequestValidator;
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

                // TODO: Implementation will be added later
                // This will download the sound file, normalize it, and return the normalized audio
                await Task.Delay(10); // Placeholder for async operation

                return StatusCode(501, "Implementation pending");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to normalize the sound.");
                throw;
            }
        }
    }
}
