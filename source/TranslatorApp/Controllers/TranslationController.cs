// Ignore Spelling: Validator req App

using CopyWords.Parsers;
using CopyWords.Parsers.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TranslatorApp.Extensions;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly ITranslationsService _translationsService;
        private readonly IValidator<LookUpWordRequest> _requestValidatorMock;
        private readonly ILookUpWord _lookUpWord;
        private readonly IWebHostEnvironment _environment;
        private readonly IGlobalSettings _globalSettings;

        public TranslationController(
            ILogger<TranslationController> logger,
            ITranslationsService translationsService,
            IValidator<LookUpWordRequest> lookUpWordRequestValidator,
            ILookUpWord lookUpWord,
            IWebHostEnvironment environment,
            IGlobalSettings globalSettings)
        {
            _logger = logger;
            _translationsService = translationsService;
            _requestValidatorMock = lookUpWordRequestValidator;
            _lookUpWord = lookUpWord;
            _environment = environment;
            _globalSettings = globalSettings;
        }

        [HttpPost]
        [Route("api/LookUpWord")]
        public async Task<ActionResult<WordModel?>> LookUpWordAsync(
            [FromBody] LookUpWordRequest lookUpWordRequest,
            [FromQuery] string? code = null)
        {
            if (lookUpWordRequest == null)
            {
                return BadRequest("Input data is null");
            }

            if (!(lookUpWordRequest.Version == "1" || lookUpWordRequest.Version == "2"))
            {
                return BadRequest("Only protocol version 1 and 2 are supported.");
            }

            if (lookUpWordRequest.Version == "2" && code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            var validation = await _requestValidatorMock.ValidateAsync(lookUpWordRequest);
            if (!validation.IsValid)
            {
                string errorMessage = validation.FormatErrorMessage();
                return BadRequest(errorMessage);
            }

            try
            {
                string sourceLanguage = lookUpWordRequest.SourceLanguage;

                // First check if the text has language specific characters - then use that language as source language
                if (_translationsService.CheckLanguageSpecificCharacters(lookUpWordRequest.Text) is (true, string lang))
                {
                    sourceLanguage = lang;
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.LanguageSpecificCharactersFound),
                        "The text '{Text}' has language specific characters, will use '{Language}' as source language.",
                        lookUpWordRequest.Text,
                        sourceLanguage);
                }

                _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupRequestReceived),
                    "Will lookup '{Text}' in the '{SourceLanguage}' dictionary.",
                    lookUpWordRequest.Text,
                    sourceLanguage);

                WordModel? wordModel = await _lookUpWord.LookUpWordAsync(lookUpWordRequest.Text, sourceLanguage);

                // If the source language is Danish and the word starts with "at ", remove "at " and search again.
                if (wordModel == null
                    && string.Equals(sourceLanguage, SourceLanguage.Danish.ToString(), StringComparison.InvariantCultureIgnoreCase)
                    && lookUpWordRequest.Text.StartsWith("at ", StringComparison.InvariantCultureIgnoreCase))
                {
                    string textWithoutAt = lookUpWordRequest.Text[3..];

                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.RemoveAtPrefix),
                        "The text '{Text}' starts with 'at ' and the destination language is '{SourceLanguage}', so it is most likely a verb. " +
                        "DDO returns 'not found' when search with 'at ', will try again searching for '{TextWithoutAt}'.",
                        lookUpWordRequest.Text,
                        sourceLanguage,
                        textWithoutAt);

                    wordModel = await _lookUpWord.LookUpWordAsync(textWithoutAt, sourceLanguage);
                }

                if (wordModel == null)
                {
                    // Try another parser - assuming that the user forgot to change the dictionary in the UI
                    string anotherLanguage = string.Equals(sourceLanguage, SourceLanguage.Danish.ToString(), StringComparison.InvariantCultureIgnoreCase)
                        ? SourceLanguage.Spanish.ToString() : SourceLanguage.Danish.ToString();

                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupRequestReceived),
                        "Word '{Text}' not found in the '{Dictionary}' dictionary. Will look it up in the '{AnotherDictionary}' dictionary.",
                        lookUpWordRequest.Text,
                        sourceLanguage,
                        anotherLanguage);
                    wordModel = await _lookUpWord.LookUpWordAsync(lookUpWordRequest.Text, anotherLanguage);
                }

                if (wordModel == null)
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.WordNotFound),
                        "Word '{Text}' not found in any dictionary.",
                        lookUpWordRequest.Text);
                    return NotFound($"Word '{lookUpWordRequest.Text}' not found.");
                }
                else
                {
                    // If the word model is not null, call the OpenAI API to translate it
                    _logger.LogInformation(new EventId(32),
                        "Will translate '{Word}' from '{SourceLanguage}' to '{DestinationLanguage}' with OpenAI API.",
                        wordModel.Word,
                        wordModel.SourceLanguage,
                        lookUpWordRequest.DestinationLanguage);

                    wordModel = await _translationsService.TranslateAsync(wordModel);

                    if (_environment.IsDevelopment())
                    {
                        _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel),
                            "Returning word model: {TranslationOutput}",
                            JsonSerializer.Serialize(wordModel, new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            }));
                    }
                    else
                    {
                        _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel),
                            "Returning word model: {@TranslationOutput}",
                            wordModel);
                    }
                }

                return wordModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to look up the word.");
                throw;
            }
        }
    }
}
