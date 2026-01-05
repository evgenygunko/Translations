// Ignore Spelling: App

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using OpenAI.Chat;
using TranslatorApp.Models;
using TranslatorApp.Models.Translation;

namespace TranslatorApp.Services
{
    public interface IOpenAITranslationService
    {
        Task<TranslationOutput?> TranslateAsync(TranslationInput translationInput, CancellationToken cancellationToken);
    }

    public class OpenAITranslationService : IOpenAITranslationService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<OpenAITranslationService> _logger;

        public OpenAITranslationService(
            ChatClient chatClient,
            ILogger<OpenAITranslationService> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        #region Public Methods

        public async Task<TranslationOutput?> TranslateAsync(TranslationInput input, CancellationToken cancellationToken)
        {
            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine($@"
Translate the following JSON data from {input.SourceLanguage} to {input.DestinationLanguage}.

The input contains a single Definition object.

1. Headword translation
Translate Definition.Headword.Text:
- Maintain the same part of speech as defined in Definition.Headword.PartOfSpeech.
- Use Definition.Headword.Meaning to guide the translation.
- Review Definition.Headword.Examples to understand usage and context.

2. Meaning translation
For each Context in Definition.Contexts, and for each Meaning in Context.Meanings, translate Meaning.Text:
- Use Meaning.PartOfSpeech as the authoritative part of speech.
- Use Context.ContextString to guide the translation.
- Review Meaning.Examples to better understand the context.
- Maintain the same part of speech in the translation.

3. Output requirements
- Return valid JSON only.
- Preserve the original JSON structure and all existing fields unless explicitly stated otherwise.
- Do not modify IDs or non-text fields.

Headword output fields:
- Add HeadwordTranslation:
  - Provide 1 to 3 translation options in {input.DestinationLanguage}, separated by commas.
- Add HeadwordTranslationEnglish:
  - Provide 1 to 3 translation options in English, separated by commas.
- Maintain the same part of speech as Definition.Headword.PartOfSpeech in both fields.
- If the part of speech is a verb, add the infinitive marker ""to"" in HeadwordTranslationEnglish.

Meaning output fields:
- Add MeaningTranslation to each Meaning object, containing the translated version of Meaning.Text.");

            // Serialize input object to JSON and append to the prompt
            string inputJson = JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
            prompt.AppendLine("Input JSON:");
            prompt.AppendLine(inputJson);

            Stopwatch stopwatch = Stopwatch.StartNew();

            TranslationOutput? openAITranslations = await CallOpenAIAsync<TranslationOutput>(prompt.ToString(), cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(new EventId((int)TranslatorAppEventId.TranslationReceived),
                    "The call to OpenAPI Chat Completions API took {TotalSeconds} seconds.",
                    stopwatch.Elapsed.TotalSeconds);

            return openAITranslations;
        }

        #endregion

        #region Private Methods

        private async Task<T?> CallOpenAIAsync<T>(string prompt, CancellationToken cancellationToken)
        {
            List<ChatMessage> messages = new List<ChatMessage>() { new UserChatMessage(prompt) };
            ChatCompletionOptions options = CreateChatCompletionOptions<T>();

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            string json = completion.Content[0].Text;

            var translations = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            return translations;
        }

        private static ChatCompletionOptions CreateChatCompletionOptions<T>()
        {
            JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };

            JsonSchemaExporterOptions exporterOptions = new()
            {
                TreatNullObliviousAsNonNullable = true,
            };

            JsonNode schema = jsonSerializerOptions.GetJsonSchemaAsNode(typeof(T), exporterOptions);

            BinaryData jsonSchema = BinaryData.FromString(schema.ToString());

            var options = new ChatCompletionOptions()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "translation_output",
                    jsonSchema: jsonSchema,
                jsonSchemaIsStrict: true)
            };

            return options;
        }

        #endregion
    }
}
