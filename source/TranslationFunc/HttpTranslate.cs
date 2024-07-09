using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TranslationsFunc.Models;
using TranslationsFunc.Services;

namespace My.Function
{
    public class HttpTranslate
    {
        private readonly ILogger _logger;
        private readonly ITranslationService _translationService;

        public HttpTranslate(
            ILoggerFactory loggerFactory,
            ITranslationService translationService)
        {
            _logger = loggerFactory.CreateLogger<HttpTranslate>();
            _translationService = translationService;
        }

        [Function("Translate")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [FromBody] TranslationInput translationInput)
        {
            if (string.IsNullOrEmpty(translationInput.SourceLanguage))
            {
                return CreateBadRequestResponse(req, "SourceLanguage cannot be null or empty");
            }

            if (string.IsNullOrEmpty(translationInput.HeadWord))
            {
                return CreateBadRequestResponse(req, "HeadWord cannot be null or empty");
            }

            if (translationInput.DestinationLanguages.Count() == 0 || translationInput.DestinationLanguages.Count() > 2)
            {
                return CreateBadRequestResponse(req, "DestinationLanguages need to at least one element and less than two.");
            }

            // todo: add a check for languages

            _logger.LogInformation($"Will translate '{translationInput.HeadWord}' from '{translationInput.SourceLanguage}' to '" + string.Join(',', translationInput.DestinationLanguages) + "'.");

            try
            {
                var translations = await _translationService.TranslateAsync(translationInput);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(translations);

                _logger.LogInformation($"Returning translations: " + JsonSerializer.Serialize(
                    translations,
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

        private static HttpResponseData CreateBadRequestResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var responseBody = new { Error = message };
            response.WriteString(JsonSerializer.Serialize(responseBody));

            return response;
        }
    }
}
