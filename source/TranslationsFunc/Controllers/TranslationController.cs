// Ignore Spelling: Validator req

using System.Text.Json;
using TranslationsFunc.Services;
using FluentValidation;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Models.Output;
using Microsoft.AspNetCore.Mvc;

namespace TranslationsFunc.Controllers
{
    [ApiController]
    [Route("api/Translate")]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IValidator<TranslationInput> _translationInput2Validator;

        public TranslationController(
            ILogger<TranslationController> logger,
            IOpenAITranslationService openAITranslationService,
            IValidator<TranslationInput> translationInput2Validator)
        {
            _logger = logger;
            _openAITranslationService = openAITranslationService;
            _translationInput2Validator = translationInput2Validator;
        }

        [HttpPost]
        public async Task<ActionResult<TranslationOutput>> TranslateAsync([FromBody] TranslationInput translationInput)
        {
            if (translationInput == null)
            {
                return BadRequest("Input data is null");
            }

            if (translationInput.Version != "2")
            {
                return BadRequest("Only protocol version 2 is supported.");
            }

            var validation = await _translationInput2Validator.ValidateAsync(translationInput);
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return BadRequest(errorMessage);
            }

            try
            {
                IEnumerable<string> headwords = translationInput.Definitions.Select(d => d.Headword.Text).Distinct();

                _logger.LogInformation(new EventId(32),
                    "Will translate '{Headwords}' from '{SourceLanguage}' to '{DestinationLanguage}' with OpenAI API.",
                    string.Join(",", headwords),
                    translationInput.SourceLanguage,
                    translationInput.DestinationLanguage);

                TranslationOutput translationOutput = await _openAITranslationService.TranslateAsync(translationInput);

                _logger.LogInformation(new EventId(33),
                    "Returning translations: {TranslationOutput}",
                    JsonSerializer.Serialize(
                        translationOutput,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));

                return translationOutput;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling translator API.");
                throw;
            }
        }

        internal string FormatValidationErrorMessage(ValidationResult validation)
        {
            string errorMessage = "Error: ";
            foreach (var failure in validation.Errors)
            {
                errorMessage += failure.ErrorMessage.TrimEnd('.') + ", ";
            }
            errorMessage = errorMessage.TrimEnd([',', ' ']) + '.';

            return errorMessage;
        }
    }
}
