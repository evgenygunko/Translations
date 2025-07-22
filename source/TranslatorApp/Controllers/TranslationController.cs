// Ignore Spelling: Validator req

using System.Text.Json;
using CopyWords.Parsers;
using CopyWords.Parsers.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Models;
using TranslatorApp.Models.Input;
using TranslatorApp.Models.Output;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly ITranslationsService _translationsService;
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IValidator<TranslationInput> _translationInput2Validator;
        private readonly IValidator<Models.Input.V1.TranslationInput> _translationInputV1Validator;
        private readonly ILookUpWord _lookUpWord;

        public TranslationController(
            ILogger<TranslationController> logger,
            ITranslationsService translationsService,
            IOpenAITranslationService openAITranslationService,
            IValidator<TranslationInput> translationInput2Validator,
            IValidator<Models.Input.V1.TranslationInput> translationInputV1Validator,
            ILookUpWord lookUpWord)
        {
            _logger = logger;
            _translationsService = translationsService;
            _openAITranslationService = openAITranslationService;
            _translationInput2Validator = translationInput2Validator;
            _translationInputV1Validator = translationInputV1Validator;
            _lookUpWord = lookUpWord;
        }

        [HttpPost]
        [Route("api/Translate")]
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

        [HttpPost]
        [Route("api/LookUpWord")]
        public async Task<ActionResult<WordModel?>> LookUpWordAsync([FromBody] Models.Input.V1.TranslationInput translationInput)
        {
            if (translationInput == null)
            {
                return BadRequest("Input data is null");
            }

            if (translationInput.Version != "1")
            {
                return BadRequest("Only protocol version 1 is supported.");
            }

            var validation = await _translationInputV1Validator.ValidateAsync(translationInput);
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return BadRequest(errorMessage);
            }

            try
            {
                _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupRequestReceived),
                    "Will lookup '{Text}' in the '{SourceLanguage}' dictionary.",
                    translationInput.Text,
                    translationInput.SourceLanguage);

                // First call the parser to get the word model from the online dictionary
                WordModel? wordModel = await _lookUpWord.LookUpWordAsync(translationInput.Text, translationInput.SourceLanguage);

                if (wordModel == null)
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.WordNotFound),
                        "Word '{Text}' not found in the dictionary, source language '{SourceLanguage}'.",
                        translationInput.Text,
                        translationInput.SourceLanguage);
                    return NotFound($"Word '{translationInput.Text}' not found.");
                }
                else
                {
                    // If the word model is not null, call the OpenAI API to translate it
                    _logger.LogInformation(new EventId(32),
                        "Will translate '{Word}' from '{SourceLanguage}' to '{DestinationLanguage}' with OpenAI API.",
                        wordModel.Word,
                        translationInput.SourceLanguage,
                        translationInput.DestinationLanguage);

                    wordModel = await _translationsService.TranslateAsync(translationInput.SourceLanguage, wordModel);

                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel),
                        "Returning word model: {TranslationOutput}",
                        JsonSerializer.Serialize(
                            wordModel,
                            new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            }));
                }

                return wordModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to look up the word.");
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
