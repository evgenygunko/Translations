// Ignore Spelling: App

using System.ClientModel;
using System.ClientModel.Primitives;
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
        Task<TranslationOutput?> TranslateAsync(TranslationInput translationInput, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> GetTranslationSuggestionsAsync(string inputText, string sourceLanguage, string destinationLanguage, CancellationToken cancellationToken);
    }

    public class OpenAITranslationService2 : IOpenAITranslationService2
    {
        private readonly ResponsesClient _openAIResponseClient;
        private readonly ILogger<OpenAITranslationService2> _logger;
        private readonly OpenAIConfiguration _openAIConfiguration;
        private readonly ILaunchDarklyService _launchDarklyService;

        public OpenAITranslationService2(
            ResponsesClient openAIResponseClient,
            ILogger<OpenAITranslationService2> logger,
            IOptions<OpenAIConfiguration> openAIConfiguration,
            ILaunchDarklyService launchDarklyService)
        {
            _openAIResponseClient = openAIResponseClient;
            _logger = logger;
            _openAIConfiguration = openAIConfiguration.Value;
            _launchDarklyService = launchDarklyService;
        }

        #region Public Methods

        public async Task<TranslationOutput?> TranslateAsync(TranslationInput input, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            TranslationOutput? openAITranslations = await CallResponseAPIAsync<TranslationOutput>(input, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(new EventId((int)TranslatorAppEventId.TranslationReceived),
                    "The call to OpenAPI Response API took {TotalSeconds} seconds.",
                    stopwatch.Elapsed.TotalSeconds);

            return openAITranslations;
        }

        public async Task<IReadOnlyList<string>> GetTranslationSuggestionsAsync(
            string inputText,
            string sourceLanguage,
            string destinationLanguage,
            CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            TranslationSuggestionsOutput? output = await CallTranslationSuggestionsResponseAPIAsync(
                inputText,
                sourceLanguage,
                destinationLanguage,
                cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(new EventId((int)TranslatorAppEventId.TranslationSuggestionsReceived),
                    "The call to OpenAPI Response API for translation suggestions took {TotalSeconds} seconds.",
                    stopwatch.Elapsed.TotalSeconds);

            return output?.results ?? [];
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

            // Get the prompt version from LaunchDarkly
            string promptVersion = _launchDarklyService.GetStringFlag("open-ai-prompt-version", "");

            // Determine if we should add the version property
            bool shouldAddVersion = !string.IsNullOrEmpty(promptVersion) && promptVersion != "current";

            // Prompt is saved in the Dashboard: https://platform.openai.com/chat
            string versionProperty = shouldAddVersion ? $@"
                        ""version"": ""{promptVersion}""," : "";

            string message = $@"
                {{
                   ""prompt"": {{
                        ""id"": ""{_openAIConfiguration.PromptId}"",{versionProperty}
                        ""variables"": {{
                            ""source_language"": ""{translationInput.SourceLanguage}"",
                            ""destination_language"": ""{translationInput.DestinationLanguage}"",
                            ""input_json"": ""{escapedInputJson}""
                        }}
                    }}
                }}";

            return message;
        }

        internal string GetTranslationSuggestionsPromptMessage(string inputText, string sourceLanguage, string destinationLanguage)
        {
            return CreatePromptMessage(
                _openAIConfiguration.SuggestionsPromptId,
                new Dictionary<string, string?>
                {
                    ["input_text"] = inputText,
                    ["source_language"] = sourceLanguage,
                    ["destination_language"] = destinationLanguage,
                });
        }

        #endregion

        #region Private Methods

        private async Task<T?> CallResponseAPIAsync<T>(TranslationInput translationInput, CancellationToken cancellationToken)
        {
            // At this point it doesn't look that it is possible to send the variables to OpenAI model with any of the strong-typed methods.
            // The only alternative is to send it as a Binary content, but then we will have to parse the response manually.
            string promptMessage = GetPromptMessage(translationInput);

            BinaryData input = BinaryData.FromString(promptMessage);
            using BinaryContent content = BinaryContent.Create(input);

            string? extractedText = await CreateResponseAndExtractTextAsync(content, cancellationToken);

            if (string.IsNullOrEmpty(extractedText))
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.NoTextFromOpenAI),
                    "No text found in the response from OpenAI.");
                return default;
            }

            // Now deserialize the extracted text as TranslationOutput
            var translations = JsonSerializer.Deserialize<T>(extractedText, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return translations;
        }

        private async Task<TranslationSuggestionsOutput?> CallTranslationSuggestionsResponseAPIAsync(
            string inputText,
            string sourceLanguage,
            string destinationLanguage,
            CancellationToken cancellationToken)
        {
            string promptMessage = GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            BinaryData input = BinaryData.FromString(promptMessage);
            using BinaryContent content = BinaryContent.Create(input);

            string? extractedText = await CreateResponseAndExtractTextAsync(content, cancellationToken);

            if (string.IsNullOrEmpty(extractedText))
            {
                _logger.LogWarning(new EventId((int)TranslatorAppEventId.NoTextFromOpenAI),
                    "No text found in the response from OpenAI.");
                return null;
            }

            return JsonSerializer.Deserialize<TranslationSuggestionsOutput>(
                extractedText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<string?> CreateResponseAndExtractTextAsync(BinaryContent content, CancellationToken cancellationToken)
        {
            var requestOptions = new RequestOptions
            {
                CancellationToken = cancellationToken
            };

            ClientResult clientResult = await _openAIResponseClient.CreateResponseAsync(content, requestOptions);

            BinaryData binaryData = clientResult.GetRawResponse().Content;
            using JsonDocument structuredJson = JsonDocument.Parse(binaryData);

            return ExtractText(structuredJson.RootElement);
        }

        private static string? ExtractText(JsonElement rootElement)
        {
            if (rootElement.TryGetProperty("output", out JsonElement outputArray) &&
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
                        return textElement.GetString();
                    }
                }
            }

            return null;
        }

        private string CreatePromptMessage(string promptId, IReadOnlyDictionary<string, string?> variables)
        {
            string promptVersion = _launchDarklyService.GetStringFlag("open-ai-prompt-version", "");
            bool shouldAddVersion = !string.IsNullOrEmpty(promptVersion) && promptVersion != "current";
            string versionProperty = shouldAddVersion ? $@"
                        ""version"": ""{promptVersion}""," : "";

            string variablesJson = string.Join("," + Environment.NewLine, variables.Select(variable =>
                $@"                            ""{variable.Key}"": {JsonSerializer.Serialize(variable.Value ?? string.Empty)}"));

            string message = $@"
                {{
                   ""prompt"": {{
                        ""id"": ""{promptId}"",{versionProperty}
                        ""variables"": {{
{variablesJson}
                        }}
                    }}
                }}";

            return message;
        }

        #endregion
    }

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
