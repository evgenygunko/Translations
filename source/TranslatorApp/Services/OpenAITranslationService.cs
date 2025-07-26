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

Focus on translating the following properties:
1.Headword.Text
2.Meaning.Text

For Headword.Text:
    - Use the related PartOfSpeech and Meaning to inform the translation.
    - Review Headword.Examples to better understand the context.
    - Maintain the same part of speech in the translation.

For Meaning.Text:
    - Use the related PartOfSpeech and Context.ContextString to guide the translation.
    - Review Meaning.Examples to understand how the word is used in context.
    - Maintain the same part of speech in the translation.

Output requirements:
    - For each Headword.Text, provide 1 to 3 translation options in the field HeadwordTranslation, separated by commas.
    - For each Headword.Text, provide 1 to 3 translation options To English in the field HeadwordTranslationEnglish, separated by commas.
    - For each Meaning.Text, if the corresponding Context.ContextString is provided, include 1 to 3 translation options in the field MeaningTranslation, also separated by commas.");

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
