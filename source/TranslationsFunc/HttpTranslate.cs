// Ignore Spelling: Validator req

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TranslationsFunc.Services;
using FluentValidation;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Models.Output;

namespace My.Function
{
    public class HttpTranslate
    {
        private readonly ILogger _logger;
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IValidator<TranslationInput> _translationInputValidator;

        public HttpTranslate(
            ILoggerFactory loggerFactory,
            IOpenAITranslationService openAITranslationService,
            IValidator<TranslationInput> translationInputValidator)
        {
            _logger = loggerFactory.CreateLogger<HttpTranslate>();
            _openAITranslationService = openAITranslationService;
            _translationInputValidator = translationInputValidator;
        }

        [Function("Translate")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            string? inputJson = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(inputJson))
            {
                return await CreateBadRequestResponseAsync(req, "Input data is null");
            }

            // clean up request manually until VS 2022 starts support this syntax: trim everything after "> {%"
            int index = inputJson.IndexOf("> {%");
            inputJson = index >= 0 ? inputJson.Substring(0, index) : inputJson;

            TranslationInput? translationInput;
            try
            {
                translationInput = JsonSerializer.Deserialize<TranslationInput>(inputJson);
            }
            catch (Exception ex)
            {
                return await CreateBadRequestResponseAsync(req, $"Cannot deserialize input data. Exception: '{ex}'");
            }

            if (translationInput == null)
            {
                return await CreateBadRequestResponseAsync(req, "Cannot deserialize input data.");
            }

            var validation = await _translationInputValidator.ValidateAsync(translationInput);
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return await CreateBadRequestResponseAsync(req, errorMessage);
            }

            return await TranslateVersionAsync(req, translationInput);
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

        private async Task<HttpResponseData> CreateBadRequestResponseAsync(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var responseBody = new { Error = message };
            await response.WriteStringAsync(JsonSerializer.Serialize(responseBody));

            _logger.LogWarning($"Returning BadRequest: '{message}'");

            return response;
        }

        private async Task<HttpResponseData> TranslateVersionAsync(HttpRequestData req, TranslationInput translationInput)
        {
            try
            {
                _logger.LogInformation(new EventId(30),
                    "Will translate '{Text}' from '{SourceLanguage}' to '{DestinationLanguages}' with OpenAI API.",
                    translationInput.Headword.Text,
                    translationInput.SourceLanguage,
                    string.Join(',', translationInput.DestinationLanguages));

                TranslationOutput translationOutput = await _openAITranslationService.TranslateAsync(translationInput);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(translationOutput);

                _logger.LogInformation(new EventId(31),
                    "Returning translations: {TranslationOutput}",
                    JsonSerializer.Serialize(
                        translationOutput,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        }));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling translator API.");
                throw;
            }
        }
    }
}
