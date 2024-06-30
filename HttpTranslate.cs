using System.Net;
using Azure.AI.Translation.Text;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace My.Function
{
    public record TranslationInput(string HeadWord, string Meaning);
    public record TranslationOutput(string? HeadWord, string? Meaning);

    public class HttpTranslate
    {
        private readonly ILogger _logger;

        public HttpTranslate(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTranslate>();
        }

        [Function("Translate")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [FromBody] TranslationInput translationInput)
        {
            _logger.LogInformation($"Will translate '{translationInput.HeadWord}'");

            try
            {
                string key = Environment.GetEnvironmentVariable("TRANSLATIONS_APP_KEY");
                string region = Environment.GetEnvironmentVariable("TRANSLATIONS_APP_REGION");
                AzureKeyCredential credential = new(key);
                TextTranslationClient client = new(credential, region);

                string? translatedHeadWord = await TranslateAsync(client, translationInput.HeadWord);
                string? translatedMeaning = await TranslateAsync(client, translationInput.Meaning);

                var response = req.CreateResponse(HttpStatusCode.OK);

                var translationOutput = new TranslationOutput(translatedHeadWord, translatedMeaning);
                await response.WriteAsJsonAsync(translationOutput);

                _logger.LogInformation($"Returning translations: " + JsonSerializer.Serialize(translationOutput));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while calling translator API.");
                throw;
            }
        }

        private async Task<string?> TranslateAsync(TextTranslationClient client, string inputText)
        {
            string sourceLanguage = "da";
            string targetLanguage = "en";

            Response<IReadOnlyList<TranslatedTextItem>> response = await client.TranslateAsync(targetLanguage, inputText, sourceLanguage).ConfigureAwait(false);
            IReadOnlyList<TranslatedTextItem> translations = response.Value;
            TranslatedTextItem? translation = translations.FirstOrDefault();

            return translation?.Translations?.FirstOrDefault()?.Text;
        }
    }
}
