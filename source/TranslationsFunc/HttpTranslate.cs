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
        private readonly IValidator<TranslationInput2> _translationInput2Validator;

        public HttpTranslate(
            ILoggerFactory loggerFactory,
            IOpenAITranslationService openAITranslationService,
            IValidator<TranslationInput> translationInputValidator,
            IValidator<TranslationInput2> translationInput2Validator)
        {
            _logger = loggerFactory.CreateLogger<HttpTranslate>();
            _openAITranslationService = openAITranslationService;
            _translationInputValidator = translationInputValidator;
            _translationInput2Validator = translationInput2Validator;
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

            if (translationInput?.Version == "1")
            {
                var validation = await _translationInputValidator.ValidateAsync(translationInput);
                if (!validation.IsValid)
                {
                    string errorMessage = FormatValidationErrorMessage(validation);
                    return await CreateBadRequestResponseAsync(req, errorMessage);
                }

                return await TranslateVersionAsync(req, translationInput);
            }

            // Try version 2
            TranslationInput2? translationInput2;
            try
            {
                translationInput2 = JsonSerializer.Deserialize<TranslationInput2>(inputJson);
            }
            catch (Exception ex)
            {
                return await CreateBadRequestResponseAsync(req, $"Cannot deserialize input data. Exception: '{ex}'");
            }

            if (translationInput2?.Version == "2")
            {
                var validation = await _translationInput2Validator.ValidateAsync(translationInput2);
                if (!validation.IsValid)
                {
                    string errorMessage = FormatValidationErrorMessage(validation);
                    return await CreateBadRequestResponseAsync(req, errorMessage);
                }

                return await TranslateVersion2Async(req, translationInput2);
            }

            return await CreateBadRequestResponseAsync(req, "Cannot deserialize input data.");
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

        private async Task<HttpResponseData> TranslateVersion2Async(HttpRequestData req, TranslationInput2 translationInput)
        {
            try
            {
                IEnumerable<string> headwords = translationInput.Definitions.Select(d => d.Headword.Text).Distinct();

                _logger.LogInformation(new EventId(32),
                    "Will translate '{Headwords}' from '{SourceLanguage}' to '{DestinationLanguages}' with OpenAI API.",
                    string.Join(",", headwords),
                    translationInput.SourceLanguage,
                    string.Join(',', translationInput.DestinationLanguages));

                TranslationOutput2 translationOutput = await _openAITranslationService.Translate2Async(translationInput);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(translationOutput);

                _logger.LogInformation(new EventId(33),
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
