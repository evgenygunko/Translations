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
        private readonly IAzureTranslationService _azureTranslationService;
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IValidator<TranslationInput> _translationInputValidator;
        private readonly IValidator<TranslationInput2> _translationInput2Validator;

        public HttpTranslate(
            ILoggerFactory loggerFactory,
            IAzureTranslationService azureTranslationService,
            IOpenAITranslationService openAITranslationService,
            IValidator<TranslationInput> translationInputValidator,
            IValidator<TranslationInput2> translationInput2Validator)
        {
            _logger = loggerFactory.CreateLogger<HttpTranslate>();
            _azureTranslationService = azureTranslationService;
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

            TranslationInput translationInput;

            try
            {
                TranslationInput2 translationInput2 = JsonSerializer.Deserialize<TranslationInput2>(inputJson)!;

                if (int.TryParse(translationInput2.Version, out int inputVersion) && inputVersion == 1)
                {
                    return await TranslateVersion2Async(req, translationInput2);
                }

                translationInput = JsonSerializer.Deserialize<TranslationInput>(inputJson)!;
            }
            catch (Exception ex)
            {
                return await CreateBadRequestResponseAsync(req, $"Cannot deserialize input data. Exception: '{ex}'");
            }

            var validation = await _translationInputValidator.ValidateAsync((TranslationInput)translationInput);
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return await CreateBadRequestResponseAsync(req, errorMessage);
            }

            try
            {
                TranslationOutput translationOutput;
                if (string.Equals(req.Query["service"], "azure", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation($"Will translate '{translationInput.Word}' from '{translationInput.SourceLanguage}' to '" + string.Join(',', translationInput.DestinationLanguages) + "' with Azure Translator Service.");
                    translationOutput = await _azureTranslationService.TranslateAsync((TranslationInput)translationInput);
                }
                else
                {
                    // By default translate with OpenAI, it should return better results because it can analyze context and requested part of speech.
                    _logger.LogInformation($"Will translate '{translationInput.Word}' from '{translationInput.SourceLanguage}' to '" + string.Join(',', translationInput.DestinationLanguages) + "' with OpenAI API.");
                    translationOutput = await _openAITranslationService.TranslateAsync((TranslationInput)translationInput);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(translationOutput);

                _logger.LogInformation($"Returning translations: " + JsonSerializer.Serialize(
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
                _logger.LogError(ex, $"Error occurred while calling translator API.");
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

        private async Task<HttpResponseData> CreateBadRequestResponseAsync(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var responseBody = new { Error = message };
            await response.WriteStringAsync(JsonSerializer.Serialize(responseBody));

            _logger.LogWarning($"Returning BadRequest: '{message}'");

            return response;
        }

        private async Task<HttpResponseData> TranslateVersion2Async(HttpRequestData req, TranslationInput2 translationInput)
        {
            // todo: merge this code into the main function when TranslationInput is not supported anymore
            var validation = await _translationInput2Validator.ValidateAsync(translationInput);
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return await CreateBadRequestResponseAsync(req, errorMessage);
            }

            try
            {
                _logger.LogInformation($"Will translate '{translationInput.Headword.Text}' from '{translationInput.SourceLanguage}' to '" + string.Join(',', translationInput.DestinationLanguages) + "' with OpenAI API.");

                TranslationOutput2 translationOutput = await _openAITranslationService.Translate2Async(translationInput);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(translationOutput);

                _logger.LogInformation($"Returning translations: " + JsonSerializer.Serialize(
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
                _logger.LogError(ex, $"Error occurred while calling translator API.");
                throw;
            }
        }
    }
}
