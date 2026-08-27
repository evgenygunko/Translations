// Ignore Spelling: Validator req App

using System.Text.Json;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TranslatorApp.Extensions;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Controllers
{
    [ApiController]
    [ApiVersion("3.0")]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly ITranslationsService _translationsService;
        private readonly IValidator<SuggestionsRequest> _suggestionsRequestValidator;
        private readonly IValidator<WordModel> _wordModelValidator;
        private readonly IWebHostEnvironment _environment;
        private readonly IGlobalSettings _globalSettings;

        internal TimeSpan TranslateRequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public TranslationController(
            ILogger<TranslationController> logger,
            ITranslationsService translationsService,
            IValidator<SuggestionsRequest> suggestionsRequestValidator,
            IValidator<WordModel> wordModelValidator,
            IWebHostEnvironment environment,
            IGlobalSettings globalSettings)
        {
            _logger = logger;
            _translationsService = translationsService;
            _suggestionsRequestValidator = suggestionsRequestValidator;
            _wordModelValidator = wordModelValidator;
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

            var validation = await _wordModelValidator.ValidateAsync(wordModel, cancellationToken);
            if (!validation.IsValid)
            {
                string errorMessage = validation.FormatErrorMessage();
                return BadRequest(errorMessage);
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
        [MapToApiVersion("3.0")]
        [Route("api/v{version:apiVersion}/[controller]/SuggestedWords")]
        public async Task<ActionResult<SuggestedWordsModel>> SuggestedWordsV3Async(
            [FromBody] SuggestionsRequest request,
            [FromQuery] string? code = null,
            CancellationToken cancellationToken = default)
        {
            if (code != _globalSettings.RequestSecretCode)
            {
                return Unauthorized();
            }

            var validation = await _suggestionsRequestValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                string errorMessage = validation.FormatErrorMessage();
                return BadRequest(errorMessage);
            }

            IEnumerable<string> suggestions = await _translationsService.GetAISuggestedWordsAsync(
                request.Text,
                request.DestinationLanguage,
                cancellationToken);

            return Ok(new SuggestedWordsModel(suggestions));
        }

    }
}
