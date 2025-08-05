// Ignore Spelling: App

using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using TranslatorApp.Models.Translation;

namespace TranslatorApp.Services
{
    public interface IOpenAITranslationService
    {
        Task<TranslationOutput> TranslateAsync(TranslationInput translationInput);
    }

    public class OpenAITranslationService : IOpenAITranslationService
    {
        private readonly ChatClient _chatClient;

        public OpenAITranslationService(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        #region Public Methods

        public async Task<TranslationOutput> TranslateAsync(TranslationInput input)
        {
            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine($@"
Translate the following JSON data from {input.SourceLanguage} to {input.DestinationLanguage}.

For each Definition in Definitions array translate the property Headword.Text:
    - Maintain the same part of speech in the translation as defined in Headword.PartOfSpeech.
    - Use the related Headword.Meaning to guide the translation.
    - Review Headword.Examples to better understand the context.

For each Definition in Definitions array, iterate over Contexts and Meanings and translate the property Meaning.Text:
    - Use the related PartOfSpeech and Context.ContextString to guide the translation.
    - Review Examples to better understand the context.
    - Maintain the same part of speech in the translation.

Output requirements:
    - For each Headword.Text, provide 1 to 3 translation options in the field HeadwordTranslation, separated by commas.
    - For each Headword.Text, provide 1 to 3 translation options to English in the field HeadwordTranslationEnglish, separated by commas.
    - Maintain the same part of speech as defined in Definition.PartOfSpeech in HeadwordTranslation and HeadwordTranslationEnglish.
    - If the part of speech is a verb, add an infinitive marker 'to' when returning a translation in the HeadwordTranslationEnglish.");

            // Serialize input object to JSON and append to the prompt
            string inputJson = JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
            prompt.AppendLine("Input JSON:");
            prompt.AppendLine(inputJson);

            TranslationOutput? openAITranslations = await CallOpenAIAsync<TranslationOutput>(prompt.ToString());

            return openAITranslations ?? new TranslationOutput([]);
        }

        #endregion

        #region Private Methods

        private async Task<T?> CallOpenAIAsync<T>(string prompt)
        {
            List<ChatMessage> messages = new List<ChatMessage>() { new UserChatMessage(prompt) };
            ChatCompletionOptions options = CreateChatCompletionOptions<T>();

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

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
