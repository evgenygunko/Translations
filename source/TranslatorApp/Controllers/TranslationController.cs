// Ignore Spelling: Validator req App

using System.Text.Json;
using CopyWords.Parsers;
using CopyWords.Parsers.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly ITranslationsService _translationsService;
        private readonly IValidator<LookUpWordRequest> _lookUpWordRequestValidator;
        private readonly ILookUpWord _lookUpWord;

        public TranslationController(
            ILogger<TranslationController> logger,
            ITranslationsService translationsService,
            IValidator<LookUpWordRequest> lookUpWordRequestValidator,
            ILookUpWord lookUpWord)
        {
            _logger = logger;
            _translationsService = translationsService;
            _lookUpWordRequestValidator = lookUpWordRequestValidator;
            _lookUpWord = lookUpWord;
        }

        [HttpPost]
        [Route("api/LookUpWord")]
        public async Task<ActionResult<WordModel?>> LookUpWordAsync([FromBody] LookUpWordRequest lookUpWordRequest)
        {
            if (lookUpWordRequest == null)
            {
                return BadRequest("Input data is null");
            }

            if (lookUpWordRequest.Version != "1")
            {
                return BadRequest("Only protocol version 1 is supported.");
            }

            var validation = await _lookUpWordRequestValidator.ValidateAsync(lookUpWordRequest);
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return BadRequest(errorMessage);
            }

            try
            {
                _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupRequestReceived),
                    "Will lookup '{Text}' in the '{SourceLanguage}' dictionary.",
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage);

                // First call the parser to get the word model from the online dictionary
                WordModel? wordModel = await _lookUpWord.LookUpWordAsync(lookUpWordRequest.Text, lookUpWordRequest.SourceLanguage);
                if (wordModel == null)
                {
                    // Try another parser - assuming that the user forgot to change the dictionary in the UI
                    SourceLanguage sourceLanguage = Enum.Parse<SourceLanguage>(lookUpWordRequest.SourceLanguage);
                    SourceLanguage anotherLanguage = sourceLanguage == SourceLanguage.Danish ? SourceLanguage.Spanish : SourceLanguage.Danish;

                    wordModel = await _lookUpWord.LookUpWordAsync(lookUpWordRequest.Text, anotherLanguage.ToString());
                }

                if (wordModel == null)
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.WordNotFound),
                        "Word '{Text}' not found in the dictionary, source language '{SourceLanguage}'.",
                        lookUpWordRequest.Text,
                        lookUpWordRequest.SourceLanguage);
                    return NotFound($"Word '{lookUpWordRequest.Text}' not found.");
                }
                else
                {
                    // If the word model is not null, call the OpenAI API to translate it
                    _logger.LogInformation(new EventId(32),
                        "Will translate '{Word}' from '{SourceLanguage}' to '{DestinationLanguage}' with OpenAI API.",
                        wordModel.Word,
                        lookUpWordRequest.SourceLanguage,
                        lookUpWordRequest.DestinationLanguage);

                    wordModel = await _translationsService.TranslateAsync(lookUpWordRequest.SourceLanguage, wordModel);

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
