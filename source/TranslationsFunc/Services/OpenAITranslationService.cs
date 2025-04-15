using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Models.Output;

namespace TranslationsFunc.Services
{
    public interface IOpenAITranslationService
    {
        Task<TranslationOutput> TranslateAsync(TranslationInput translationInput);

        Task<TranslationOutput2> Translate2Async(TranslationInput2 translationInput);
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
            // Translate headword
            string promptForHeadword = CreatePromptForHeadword(
                    word: input.Headword.Text,
                    sourceLanguage: input.SourceLanguage,
                    destinationLanguages: input.DestinationLanguages,
                    meaning: input.Headword.Meaning,
                    partOfSpeech: input.Headword.PartOfSpeech,
                    examples: input.Headword.Examples);

            OpenAIHeadwordTranslations? openAIHeadwordTranslations = await CallOpenAIAsync<OpenAIHeadwordTranslations>(promptForHeadword);

            // Translate meanings
            string promptForMeanings = CreatePromptForMeanings(
                    sourceLanguage: input.SourceLanguage,
                    destinationLanguages: input.DestinationLanguages,
                    meanings: input.Meanings);

            OpenAIMeaningsTranslations? openAIMeaningsTranslations = await CallOpenAIAsync<OpenAIMeaningsTranslations>(promptForMeanings);

            return new TranslationOutput(
                Headword: openAIHeadwordTranslations?.Translations ?? [],
                Meanings: openAIMeaningsTranslations?.Translations ?? []);
        }

        public async Task<TranslationOutput2> Translate2Async(TranslationInput2 input)
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
    - For each Meaning.Text, if the corresponding Context.ContextString is provided, include 1 to 3 translation options in the field MeaningTranslation, also separated by commas.");

            // Serialize input object to JSON and append to the prompt
            string inputJson = JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });
            prompt.AppendLine("Input JSON:");
            prompt.AppendLine(inputJson);

            TranslationOutput2? openAITranslations = await CallOpenAIAsync<TranslationOutput2>(prompt.ToString());

            return openAITranslations ?? new TranslationOutput2([]);
        }

        #endregion

        #region Internal Methods

        internal static string CreatePromptForMeanings(
            string sourceLanguage,
            IEnumerable<string> destinationLanguages,
            IEnumerable<Models.Input.Meaning> meanings)
        {
            string formattedLanguages = string.Join(", ", destinationLanguages.Select(lang => $"'{lang}'"));

            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine($"Translate strings from the language '{sourceLanguage}' into the languages {formattedLanguages}.");
            prompt.AppendLine("Check examples to better understand the context.");
            prompt.AppendLine("In the output, retain the ID of the input text when returning translations.");

            foreach (var meaning in meanings)
            {
                string examplesFlat = "'" + string.Join("', '", meaning.Examples) + "'";
                prompt.AppendLine($"id=\"{meaning.id}\", text=\"{meaning.Text}\", examples=\"{examplesFlat}\".");
            }

            return prompt.ToString().Trim();
        }

        internal static string CreatePromptForHeadword(
            string word,
            string sourceLanguage,
            IEnumerable<string> destinationLanguages,
            string meaning,
            string partOfSpeech,
            IEnumerable<string> examples)
        {
            string formattedLanguages = string.Join(", ", destinationLanguages.Select(lang => $"'{lang}'"));

            StringBuilder prompt = new StringBuilder();
            prompt.Append($"Translate the word '{word}' from the language '{sourceLanguage}' into the languages {formattedLanguages}");
            if (!string.IsNullOrEmpty(partOfSpeech))
            {
                prompt.Append($", where the part of speech is: '{partOfSpeech}'");
            }
            prompt.AppendLine(".");

            if (!string.IsNullOrEmpty(meaning))
            {
                prompt.AppendLine($"The word, in this context, means: '{meaning}'.");
            }

            if (examples?.Count() > 0)
            {
                string examplesFlat = "'" + string.Join("', '", examples) + "'";
                prompt.AppendLine($"Check examples to better understand the context: {examplesFlat}.");
            }

            prompt.AppendLine("Provide between 1 and 3 possible answers so I can choose the best one.");

            if (!string.IsNullOrEmpty(partOfSpeech))
            {
                prompt.AppendLine("Maintain the same part of speech in the translations.");
            }

            prompt.Append("When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");

            return prompt.ToString();
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
