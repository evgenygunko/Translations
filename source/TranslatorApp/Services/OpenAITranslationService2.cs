// Ignore Spelling: App

using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using TranslatorApp.Models;
using TranslatorApp.Models.Translation;

namespace TranslatorApp.Services
{
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public interface IOpenAITranslationService2
    {
        Task<TranslationOutput> TranslateAsync(TranslationInput translationInput);
    }

    public class OpenAITranslationService2 : IOpenAITranslationService2
    {
        private readonly OpenAIResponseClient _openAIResponseClient;
        private readonly ILogger<OpenAITranslationService2> _logger;
        private readonly OpenAIConfiguration _openAIConfiguration;

        public OpenAITranslationService2(
            OpenAIResponseClient openAIResponseClient,
            ILogger<OpenAITranslationService2> logger,
            IOptions<OpenAIConfiguration> openAIConfiguration)
        {
            _openAIResponseClient = openAIResponseClient;
            _logger = logger;
            _openAIConfiguration = openAIConfiguration.Value;
        }

        #region Public Methods

        public async Task<TranslationOutput> TranslateAsync(TranslationInput input)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            TranslationOutput? openAITranslations = await CallResponseAPIAsync<TranslationOutput>(input);

            stopwatch.Stop();
            _logger.LogInformation(new EventId((int)TranslatorAppEventId.TranslationReceived),
                    "The call to OpenAPI Response API took {TotalSeconds} seconds.",
                    stopwatch.Elapsed.TotalSeconds);

            return openAITranslations ?? new TranslationOutput([]);
        }

        #endregion

        #region Internal Methods

        internal string GetPromptMessage(TranslationInput translationInput)
        {
            // At this point it doesn't look that it is possible to send the variables to OpenAI model with any of the strong-typed methods.
            // The only alternative is to send it as a Binary content, but then we will have to parse the response manually.
            string inputJson = JsonSerializer.Serialize(translationInput, new JsonSerializerOptions { WriteIndented = true });

            // Properly escape the inputJson string for embedding in JSON
            string escapedInputJson = JsonSerializer.Serialize(inputJson);
            escapedInputJson = escapedInputJson[1..^1];

            // Prompt is saved in the Dashboard: https://platform.openai.com/chat
            string message = $@"
                {{
                   ""prompt"": {{
                        ""id"": ""{_openAIConfiguration.PromptId}"",
                        ""variables"": {{
                            ""source_language"": ""{translationInput.SourceLanguage}"",
                            ""destination_language"": ""{translationInput.DestinationLanguage}"",
                            ""input_json"": ""{escapedInputJson}""
                        }}
                    }}
                }}";

            return message;
        }

        #endregion

        #region Private Methods

        private async Task<T?> CallResponseAPIAsync<T>(TranslationInput translationInput)
        {
            // At this point it doesn't look that it is possible to send the variables to OpenAI model with any of the strong-typed methods.
            // The only alternative is to send it as a Binary content, but then we will have to parse the response manually.
            string promptMessage = GetPromptMessage(translationInput);

            BinaryData input = BinaryData.FromString(promptMessage);
            using BinaryContent content = BinaryContent.Create(input);

            ClientResult clientResult = await _openAIResponseClient.CreateResponseAsync(content);

            BinaryData binaryData = clientResult.GetRawResponse().Content;
            using JsonDocument structuredJson = JsonDocument.Parse(binaryData);

            // Extract the text from output[0].content[0].text path
            string? extractedText = null;
            if (structuredJson.RootElement.TryGetProperty("output", out JsonElement outputArray) &&
                outputArray.ValueKind == JsonValueKind.Array &&
                outputArray.GetArrayLength() > 0)
            {
                JsonElement firstOutput = outputArray[0];
                if (firstOutput.TryGetProperty("content", out JsonElement contentArray) &&
                    contentArray.ValueKind == JsonValueKind.Array &&
                    contentArray.GetArrayLength() > 0)
                {
                    JsonElement firstContent = contentArray[0];
                    if (firstContent.TryGetProperty("text", out JsonElement textElement))
                    {
                        extractedText = textElement.GetString();
                    }
                }
            }

            if (string.IsNullOrEmpty(extractedText))
            {
                _logger.LogWarning("No text found in the response from OpenAI.");
                return default;
            }

            // Now deserialize the extracted text as TranslationOutput
            var translations = JsonSerializer.Deserialize<T>(extractedText, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return translations;
        }

        #endregion
    }

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
