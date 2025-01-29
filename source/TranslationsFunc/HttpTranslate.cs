// Ignore Spelling: Validator req

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TranslationsFunc.Models;
using TranslationsFunc.Services;
using FluentValidation;
using FluentValidation.Results;

namespace My.Function
{
    public class HttpTranslate
    {
        private readonly ILogger _logger;
        private readonly IAzureTranslationService _azureTranslationService;
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IValidator<TranslationInput> _translationInputValidator;

        public HttpTranslate(
            ILoggerFactory loggerFactory,
            IAzureTranslationService azureTranslationService,
            IOpenAITranslationService openAITranslationService,
            IValidator<TranslationInput> translationInputValidator)
        {
            _logger = loggerFactory.CreateLogger<HttpTranslate>();
            _azureTranslationService = azureTranslationService;
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

#pragma warning disable CS8604 // Possible null reference argument.
            var validation = await _translationInputValidator.ValidateAsync(translationInput);
#pragma warning restore CS8604 // Possible null reference argument.
            if (!validation.IsValid)
            {
                string errorMessage = FormatValidationErrorMessage(validation);
                return await CreateBadRequestResponseAsync(req, errorMessage);
            }

            TranslationOutput translationOutput;
            try
            {
                if (string.Equals(req.Query["service"], "azure", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogInformation($"Will translate '{translationInput.Word}' from '{translationInput.SourceLanguage}' to '" + string.Join(',', translationInput.DestinationLanguages) + "' with Azure Translator Service.");
                    translationOutput = await _azureTranslationService.TranslateAsync(translationInput);
                }
                else
                {
                    // By default translate with OpenAI, it should return better results because it can analyze context and requested part of speech.
                    _logger.LogInformation($"Will translate '{translationInput.Word}' from '{translationInput.SourceLanguage}' to '" + string.Join(',', translationInput.DestinationLanguages) + "' with OpenAI API.");
                    translationOutput = await _openAITranslationService.TranslateAsync(translationInput);
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
    }
}
