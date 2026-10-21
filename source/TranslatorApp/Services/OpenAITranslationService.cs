// Ignore Spelling: App

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using OpenAI.Chat;
using TranslatorApp.Exceptions;
using TranslatorApp.Models;
using TranslatorApp.Models.Translation;

namespace TranslatorApp.Services
{
    public interface IOpenAITranslationService
    {
        Task<TranslationOutput?> TranslateAsync(TranslationInput translationInput, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> GetTranslationSuggestionsAsync(string inputText, string sourceLanguage, string destinationLanguage, CancellationToken cancellationToken);
    }

    public class OpenAITranslationService : IOpenAITranslationService
    {
        public const string UseGpt6LunaFlag = "use-gpt-6-luna";

        private readonly ChatClient _legacyChatClient;
        private readonly ChatClient _lunaChatClient;
        private readonly ILaunchDarklyService _launchDarklyService;
        private readonly ILogger<OpenAITranslationService> _logger;

        public OpenAITranslationService(
            [FromKeyedServices("gpt-4.1-mini")] ChatClient legacyChatClient,
            [FromKeyedServices("gpt-6-luna")] ChatClient lunaChatClient,
            ILaunchDarklyService launchDarklyService,
            ILogger<OpenAITranslationService> logger)
        {
            _legacyChatClient = legacyChatClient;
            _lunaChatClient = lunaChatClient;
            _launchDarklyService = launchDarklyService;
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

            string model = GetModel();
            TranslationOutput? openAITranslations = await CallOpenAIAsync<TranslationOutput>(prompt.ToString(), model, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(new EventId((int)TranslatorAppEventId.TranslationReceived),
                    "The call to OpenAI Chat Completions API using {Model} took {TotalSeconds} seconds.",
                    model, stopwatch.Elapsed.TotalSeconds);

            return openAITranslations;
        }

        public async Task<IReadOnlyList<string>> GetTranslationSuggestionsAsync(
            string inputText,
            string sourceLanguage,
            string destinationLanguage,
            CancellationToken cancellationToken)
        {
            string prompt = $@"
Translate the input text from source language to destination language. Find the values below.

Provide up to 10 valid translation results.
Include plausible synonymous senses or homonyms when relevant, but do not invent unlikely meanings.
Order results by commonness and relevance, with the most common or likely translation first.

Formatting rules:
- Return a JSON object only.
- The object must contain exactly one required property named ""results"".
- ""results"" must be an array of strings.
- Each string must contain exactly one translation candidate in destination_language.
- Do not include explanations, numbering, usage notes, or duplicate items.
- If the destination language is Danish:
  - Prefix verbs with ""at "".
  - For nouns, include the correct indefinite article: ""en "" or ""et "".
- If the destination language is Spanish:
  - For nouns, include the correct indefinite article: ""un "" or ""una"".
- If source_language or destination_language is unknown or unsupported, if input_text is empty, or if no valid translation candidates exist, return {"results":[]}.

input text: ""{inputText}""
source language: ""{sourceLanguage}""
destination language: ""{destinationLanguage}""";

            Stopwatch stopwatch = Stopwatch.StartNew();

            string model = GetModel();
            TranslationSuggestionsOutput? response = await CallOpenAIAsync<TranslationSuggestionsOutput>(prompt, model, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(new EventId((int)TranslatorAppEventId.TranslationSuggestionsReceived),
                    "The call to OpenAI Chat Completions API using {Model} took {TotalSeconds} seconds.",
                    model, stopwatch.Elapsed.TotalSeconds);

            return response?.results ?? [];
        }

        #endregion

        #region Private Methods

        private string GetModel()
        {
            try
            {
                return _launchDarklyService.GetBooleanFlag(UseGpt6LunaFlag, defaultValue: false)
                    ? "gpt-6-luna" : "gpt-4.1-mini";
            }
            catch (LaunchDarklyGetFlagException ex)
            {
                _logger.LogWarning(ex, "Could not evaluate {Flag}; using gpt-4.1-mini", UseGpt6LunaFlag);
                return "gpt-4.1-mini";
            }
        }

        private async Task<T?> CallOpenAIAsync<T>(string prompt, string model, CancellationToken cancellationToken)
        {
            List<ChatMessage> messages = new List<ChatMessage>() { new UserChatMessage(prompt) };
            ChatCompletionOptions options = CreateChatCompletionOptions<T>();
            bool useLuna = model == "gpt-6-luna";

            if (useLuna)
            {
#pragma warning disable OPENAI001 // Reasoning controls are experimental in the pinned OpenAI SDK.
                options.ReasoningEffortLevel = new ChatReasoningEffortLevel("none");
#pragma warning restore OPENAI001
            }

            ChatClient chatClient = useLuna ? _lunaChatClient : _legacyChatClient;
            _logger.LogInformation("Calling OpenAI Chat Completions API using {Model}", model);
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

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
