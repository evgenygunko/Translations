using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace My.Function
{
    public record TranslationInput(string HeadWord, string Meaning);
    public record TranslationOutput(string HeadWord, string Meaning);

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


            var response = req.CreateResponse(HttpStatusCode.OK);

            var translationOutput = new TranslationOutput(
                $"TODO: {translationInput.HeadWord}",
                $"TODO: {translationInput.Meaning}");
            await response.WriteAsJsonAsync(translationOutput);

            return response;
        }
    }
}
