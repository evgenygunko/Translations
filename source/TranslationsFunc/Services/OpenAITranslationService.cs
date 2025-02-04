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
    public interface IOpenAITranslationService : ITranslationService
    {
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
            var prompt = CreatePrompt(input);
            TranslationOutput? translationOutput = await TranslateHeadwordAsync(prompt);

            return translationOutput ?? new TranslationOutput(Array.Empty<TranslationItem>());
        }

        public async Task<TranslationOutput2> Translate2Async(TranslationInput2 input)
        {
            // Translate headword
            string promptForHeadword = CreatePromptForHeadword(
                    word: input.Headword.Text,
                    sourceLanguage: input.SourceLanguage,
                    destinationLanguages: input.DestinationLanguages,
                    meaning: input.Headword.Meaning,
                    partOfSpeech: input.Headword.PartOfSpeech,
                    examples: input.Headword.Examples);

            TranslationOutput? headWordTranslationsOutput = await TranslateHeadwordAsync(promptForHeadword);

            var headwords = new List<Models.Output.Headword>();
            if (headWordTranslationsOutput != null)
            {
                for (int i = 0; i < headWordTranslationsOutput.Translations.Length; i++)
                {
                    headwords.Add(new Models.Output.Headword(
                                Language: headWordTranslationsOutput.Translations[i].Language,
                                HeadwordTranslations: headWordTranslationsOutput.Translations[i].TranslationVariants));
                }
            }

            // Translate meanings
            string promptForMeanings = CreatePromptForMeanings(
                    sourceLanguage: input.SourceLanguage,
                    destinationLanguages: input.DestinationLanguages,
                    meanings: input.Meanings);

            return new TranslationOutput2(
                Headword: headwords.ToArray(),
                Meanings: []);
        }

        #endregion

        #region Internal Methods

        internal string CreatePrompt(TranslationInput input)
        {
            string prompt;
            if (string.IsNullOrEmpty(input.Word) && !string.IsNullOrEmpty(input.Meaning))
            {
                prompt = CreatePromptForMeaning(
                    sourceLanguage: input.SourceLanguage,
                    destinationLanguages: input.DestinationLanguages,
                    meaning: input.Meaning,
                    examples: input.Examples);
            }
            else
            {
                prompt = CreatePromptForHeadword(
                    word: input.Word,
                    sourceLanguage: input.SourceLanguage,
                    destinationLanguages: input.DestinationLanguages,
                    meaning: input.Meaning,
                    partOfSpeech: input.PartOfSpeech,
                    examples: input.Examples);
            }

            return prompt;
        }

        #endregion

        #region Internal Methods

        internal static string CreatePromptForMeaning(
            string sourceLanguage,
            IEnumerable<string> destinationLanguages,
            string meaning,
            IEnumerable<string> examples)
        {
            string formattedLanguages = string.Join(", ", destinationLanguages.Select(lang => $"'{lang}'"));

            var prompt = $"Translate '{meaning}' from the language '{sourceLanguage}' into the languages {formattedLanguages}. ";

            if (examples?.Count() > 0)
            {
                string examplesFlat = "'" + string.Join("', '", examples) + "'";
                prompt += $"Check also examples to get a better context: {examplesFlat}.";
            }

            return prompt;
        }

        internal static string CreatePromptForMeanings(
            string sourceLanguage,
            IEnumerable<string> destinationLanguages,
            IEnumerable<Models.Input.Meaning> meanings)
        {
            string formattedLanguages = string.Join(", ", destinationLanguages.Select(lang => $"'{lang}'"));

            StringBuilder prompt = new StringBuilder();
            prompt.AppendLine($"Translate strings from the language '{sourceLanguage}' into the languages {formattedLanguages}.");
            prompt.AppendLine("Check also examples to get a better context.");
            prompt.AppendLine("For the output, keep id of the input text when returning translations.");

            foreach (var meaning in meanings)
            {
                string examplesFlat = "'" + string.Join("', '", meaning.Examples) + "'";
                prompt.AppendLine($"id=\"{meaning.id}\", text=\"{meaning.Text}\", examples=\"{examplesFlat}\".");
            }

            return prompt.ToString();
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

            string partOfSpeechPlaceholder = !string.IsNullOrEmpty(partOfSpeech) ? $", where the part of speech is: '{partOfSpeech}'" : "";
            string meaningPlaceholder = !string.IsNullOrEmpty(meaning) ? $"The word, in this context, means: '{meaning}'. " : "";

            var prompt = $"Translate the word '{word}' from the language '{sourceLanguage}' into the languages {formattedLanguages}{partOfSpeechPlaceholder}. "
                + $"{meaningPlaceholder}Provide between 1 and 3 possible answers so I can choose the best one. ";

            if (examples?.Count() > 0)
            {
                string examplesFlat = "'" + string.Join("', '", examples) + "'";
                prompt += $"Check also examples to get a better context: {examplesFlat}. ";
            }

            if (!string.IsNullOrEmpty(partOfSpeech))
            {
                prompt += "Maintain the same part of speech in the translations. ";
            }

            prompt += "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.";
            return prompt;
        }

        #endregion

        #region Private Methods

        private async Task<TranslationOutput?> TranslateHeadwordAsync(string prompt)
        {
            List<ChatMessage> messages = new List<ChatMessage>() { new UserChatMessage(prompt) };
            ChatCompletionOptions options = CreateChatCompletionOptions<TranslationOutput>();

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            string json = completion.Content[0].Text;

            var jsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            var translationOutput = JsonSerializer.Deserialize<TranslationOutput>(json, jsonOptions);
            return translationOutput;
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
