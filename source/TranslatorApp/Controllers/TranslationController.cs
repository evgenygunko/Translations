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

        internal TimeSpan LookupRequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
        internal TimeSpan TranslateRequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

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
            [FromQuery] string? code = null,
            CancellationToken cancellationToken = default)
        {
            if (lookUpWordRequest == null)
            {
                return BadRequest("Input data is null");
            }

            if (lookUpWordRequest.Version != "2")
            {
                return BadRequest("Only protocol version 2 is supported.");
            }

            if (code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            var validation = await _requestValidatorMock.ValidateAsync(lookUpWordRequest, cancellationToken);
            if (!validation.IsValid)
            {
                string errorMessage = validation.FormatErrorMessage();
                return BadRequest(errorMessage);
            }

            CancellationToken? lookupRequestCt = null;
            CancellationToken? translateRequestCt = null;

            try
            {
                lookupRequestCt = new CancellationTokenSource(LookupRequestTimeout).Token;
                using var lookupLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lookupRequestCt.Value);

                WordModel? wordModel = await _translationsService.LookUpWordInDictionaryAsync(
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage,
                    lookUpWordRequest.DestinationLanguage,
                    lookupLinkedCts.Token);

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

                translateRequestCt = new CancellationTokenSource(TranslateRequestTimeout).Token;
                using var translateLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, translateRequestCt.Value);

                wordModel = await _translationsService.TranslateAsync(wordModel, translateLinkedCts.Token);

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
            catch (OperationCanceledException) when (lookupRequestCt?.IsCancellationRequested == true)
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.CallingOnlineDictionaryTimedOut),
                    "Calling online dictionary timed out after {Timeout} seconds", LookupRequestTimeout.TotalSeconds);
                return StatusCode(504, "Translation timed out");
            }
            catch (OperationCanceledException) when (translateRequestCt?.IsCancellationRequested == true)
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.CallingOpenAITimeoudOut),
                    "Calling OpenAI API timed out after {Timeout} seconds", TranslateRequestTimeout.TotalSeconds);
                return StatusCode(504, "Translation timed out");
            }
            catch (OperationCanceledException)
            {
                // Client cancelled the request - don't log as error
                throw;
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
