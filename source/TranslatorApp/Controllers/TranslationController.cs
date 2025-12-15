// Ignore Spelling: Validator req App

using System.Text.Json;
using CopyWords.Parsers.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IWebHostEnvironment _environment;
        private readonly IGlobalSettings _globalSettings;

        public TranslationController(
            ILogger<TranslationController> logger,
            ITranslationsService translationsService,
            IValidator<LookUpWordRequest> lookUpWordRequestValidator,
            IWebHostEnvironment environment,
            IGlobalSettings globalSettings)
        {
            _logger = logger;
            _translationsService = translationsService;
            _requestValidatorMock = lookUpWordRequestValidator;
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
                WordModel? wordModel = await _translationsService.LookUpWordInDictionaryAsync(
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage,
                    lookUpWordRequest.DestinationLanguage);

                if (wordModel == null)
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.WordNotFound),
                        "Word '{Text}' not found in any dictionary.",
                        lookUpWordRequest.Text);
                    return NotFound($"Word '{lookUpWordRequest.Text}' not found.");
                }

                // Call the OpenAI API to translate it
                _logger.LogInformation(new EventId((int)TranslatorAppEventId.WillTranslateWithOpenAI),
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

                return wordModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId((int)TranslatorAppEventId.ErrorDuringLookup),
                    ex, "An error occurred while trying to look up the word.");
                throw;
            }
        }
    }
}
