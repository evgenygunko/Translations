// Ignore Spelling: Validator req App

using System.Net;
using System.Text.Json;
using Asp.Versioning;
using CopyWords.Parsers;
using CopyWords.Parsers.Exceptions;
using CopyWords.Parsers.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Extensions;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
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
        [MapToApiVersion("3.0")]
        [Route("api/v{version:apiVersion}/[controller]/LookUpWord")]
        public async Task<ActionResult<WordModel?>> LookUpWordV3Async(
            [FromBody] WordModel wordModel,
            [FromQuery] string? code = null,
            CancellationToken cancellationToken = default)
        {
            if (code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            if (!IsUsable(wordModel))
            {
                return BadRequest("Word model is missing or structurally unusable");
            }

            CancellationToken? translateRequestCt = null;

            try
            {
                string sourceLanguage = wordModel.SourceLanguage.ToString();
                const string destinationLanguage = "Russian";

                _logger.LogInformation(new EventId((int)TranslatorAppEventId.WillTranslateWithOpenAI),
                    "Will translate '{Word}' from '{SourceLanguage}' to '{DestinationLanguage}' with OpenAI API.",
                    wordModel.Word,
                    sourceLanguage,
                    destinationLanguage);

                translateRequestCt = new CancellationTokenSource(TranslateRequestTimeout).Token;
                using var translateLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, translateRequestCt.Value);

                WordModel translatedWordModel = await _translationsService.TranslateAsync(
                    wordModel,
                    sourceLanguage,
                    destinationLanguage,
                    translateLinkedCts.Token);

                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel2),
                        "Returning word model: {TranslationOutput}",
                        JsonSerializer.Serialize(translatedWordModel, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));
                }
                else
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel2),
                        "Returning word model: {@TranslationOutput}",
                        translatedWordModel);
                }

                return translatedWordModel;
            }
            catch (OperationCanceledException) when (translateRequestCt?.IsCancellationRequested == true)
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.CallingOpenAITimeoudOut),
                    "Calling OpenAI API timed out after {Timeout} seconds", TranslateRequestTimeout.TotalSeconds);
                return StatusCode(500, "Translation timed out");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(new EventId((int)TranslatorAppEventId.ErrorDuringLookup),
                    ex, "An error occurred while trying to translate the supplied word model. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, $"An internal error occurred. CorrelationId: {correlationId}");
            }
        }

        [HttpPost]
        [MapToApiVersion("2.0")]
        [Route("api/v{version:apiVersion}/[controller]/LookUpWord")]
        public async Task<ActionResult<WordModel?>> LookUpWordAsync(
            [FromBody] LookUpWordRequest lookUpWordRequest,
            [FromQuery] string? code = null,
            CancellationToken cancellationToken = default)
        {
            if (lookUpWordRequest == null)
            {
                return BadRequest("Input data is null");
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

                _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupControllerRequestReceived),
                    "TranslationController received lookup request. Text: '{Text}', SourceLanguage: '{SourceLanguage}', DestinationLanguage: '{DestinationLanguage}', ActiveDictionaries: {@ActiveDictionaries}.",
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage,
                    lookUpWordRequest.DestinationLanguage,
                    lookUpWordRequest.ActiveDictionaries);

                WordModel? wordModel = await _translationsService.LookUpWordInDictionaryAsync(
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage,
                    lookUpWordRequest.DestinationLanguage,
                    lookUpWordRequest.ActiveDictionaries,
                    lookupLinkedCts.Token);

                if (wordModel == null)
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.WordNotFound),
                        "Word '{Text}' not found in any dictionary.",
                        lookUpWordRequest.Text);
                    return NotFound($"Word '{lookUpWordRequest.Text}' not found.");
                }

                // Call the OpenAI API to translate it
                // hack: if the request URL contains a link to English word, set the source language to English.
                string sourceLanguage = lookUpWordRequest.SourceLanguage;
                if (wordModel.SourceLanguage == SourceLanguage.Spanish && lookUpWordRequest.Text.EndsWith("?langFrom=en"))
                {
                    sourceLanguage = "English";
                }

                _logger.LogInformation(new EventId((int)TranslatorAppEventId.WillTranslateWithOpenAI),
                    "Will translate '{Word}' from '{SourceLanguage}' to '{DestinationLanguage}' with OpenAI API.",
                    wordModel.Word,
                    sourceLanguage,
                    lookUpWordRequest.DestinationLanguage);

                translateRequestCt = new CancellationTokenSource(TranslateRequestTimeout).Token;
                using var translateLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, translateRequestCt.Value);

                wordModel = await _translationsService.TranslateAsync(wordModel, sourceLanguage, lookUpWordRequest.DestinationLanguage, translateLinkedCts.Token);

                if (_environment.IsDevelopment())
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel2),
                        "Returning word model: {TranslationOutput}",
                        JsonSerializer.Serialize(wordModel, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));
                }
                else
                {
                    _logger.LogInformation(new EventId((int)TranslatorAppEventId.ReturningWordModel2),
                        "Returning word model: {@TranslationOutput}",
                        wordModel);
                }

                return wordModel;
            }
            catch (OperationCanceledException) when (lookupRequestCt?.IsCancellationRequested == true)
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.CallingOnlineDictionaryTimedOut),
                    "Calling online dictionary timed out after {Timeout} seconds", LookupRequestTimeout.TotalSeconds);
                return StatusCode(
                    (int)HttpStatusCode.InternalServerError,
                    "The online dictionary did not respond in time and may be experiencing a temporary problem. Please try again later.");
            }
            catch (OperationCanceledException) when (translateRequestCt?.IsCancellationRequested == true)
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.CallingOpenAITimeoudOut),
                    "Calling OpenAI API timed out after {Timeout} seconds", TranslateRequestTimeout.TotalSeconds);
                return StatusCode(500, "Translation timed out");
            }
            catch (OperationCanceledException)
            {
                // Client cancelled the request - don't log as error
                throw;
            }
            catch (ServerErrorException ex) when (
                ex.StatusCode == HttpStatusCode.ServiceUnavailable
                || ex.StatusCode == HttpStatusCode.BadGateway
                || ex.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                string dictionaryName = ResolveDictionaryName(ex.RequestUrl);
                string message = $"Online dictionary '{dictionaryName}' is temporarily unavailable. Original error: {ex.Message}";

                _logger.LogWarning(new EventId((int)TranslatorAppEventId.OnlineDictionaryUnavailable),
                    ex,
                    "{Message}",
                    message);

                return StatusCode((int)HttpStatusCode.InternalServerError, message);
            }
            catch (Exception ex)
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogError(new EventId((int)TranslatorAppEventId.ErrorDuringLookup),
                    ex, "An error occurred while trying to look up the word. CorrelationId: {CorrelationId}", correlationId);
                return StatusCode(500, $"An internal error occurred. CorrelationId: {correlationId}");
            }
        }

        [HttpPost]
        [MapToApiVersion("2.0")]
        [Route("api/v{version:apiVersion}/[controller]/SuggestedWords")]
        public async Task<ActionResult<SuggestedWordsModel>> SuggestedWordsAsync(
            [FromBody] LookUpWordRequest lookUpWordRequest,
            [FromQuery] string? code = null,
            CancellationToken cancellationToken = default)
        {
            if (lookUpWordRequest == null)
            {
                return BadRequest("Input data is null");
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

            IEnumerable<string> suggestions = await _translationsService.GetSuggestedWordsAsync(
                lookUpWordRequest.Text,
                lookUpWordRequest.SourceLanguage,
                cancellationToken);

            return Ok(new SuggestedWordsModel(suggestions));
        }

        private static string ResolveDictionaryName(string? requestUrl)
        {
            if (string.IsNullOrEmpty(requestUrl))
            {
                return "Unknown";
            }

            if (requestUrl.StartsWith(DDOPageParser.DDOBaseUrl, StringComparison.CurrentCultureIgnoreCase))
            {
                return "DDO";
            }

            if (requestUrl.StartsWith(SpanishDictPageParser.SpanishDictBaseUrl, StringComparison.CurrentCultureIgnoreCase))
            {
                return "SpanishDict";
            }

            return "Unknown";
        }

        private static bool IsUsable(WordModel? wordModel)
        {
            return wordModel != null
                && !string.IsNullOrWhiteSpace(wordModel.Word)
                && Enum.IsDefined(wordModel.SourceLanguage)
                && wordModel.Definition != null
                && wordModel.Definition.Headword != null
                && !string.IsNullOrWhiteSpace(wordModel.Definition.Headword.Original)
                && wordModel.Definition.Contexts != null
                && wordModel.Definition.Contexts.Any()
                && wordModel.Definition.Contexts.All(context =>
                    context != null
                    && context.Meanings != null
                    && context.Meanings.All(meaning =>
                        meaning != null
                        && !string.IsNullOrWhiteSpace(meaning.Original)
                        && meaning.Examples != null))
                && wordModel.Variants != null
                && wordModel.Expressions != null;
        }
    }
}
