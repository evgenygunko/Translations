using OpenAI.Chat;
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
        Task<TranslationOutput> Translate2Async(TranslationInput2 translationInput);
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

            List<ChatMessage> messages = new List<ChatMessage>() { new UserChatMessage(prompt) };
            ChatCompletionOptions options = CreateChatCompletionOptions();

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            string json = completion.Content[0].Text;

            var jsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            var translationOutput = JsonSerializer.Deserialize<TranslationOutput>(json, jsonOptions);

            return translationOutput ?? new TranslationOutput(Array.Empty<TranslationItem>());
        }

        public async Task<TranslationOutput> Translate2Async(TranslationInput2 translationInput)
        {
            // todo: to implement
            //var prompt = CreatePrompt2(input);
            string prompt = "Translate2Async";

            List<ChatMessage> messages = new List<ChatMessage>() { new UserChatMessage(prompt) };
            ChatCompletionOptions options = CreateChatCompletionOptions();

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            string json = completion.Content[0].Text;

            var jsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            var translationOutput = JsonSerializer.Deserialize<TranslationOutput>(json, jsonOptions);

            return translationOutput ?? new TranslationOutput(Array.Empty<TranslationItem>());
        }

        #endregion

        #region Internal Methods

        internal string CreatePrompt(TranslationInput input)
        {
            string formattedLanguages = string.Join(", ", input.DestinationLanguages.Select(lang => $"'{lang}'"));

            string prompt;
            if (string.IsNullOrEmpty(input.Word) && !string.IsNullOrEmpty(input.Meaning))
            {
                prompt = CreatePromptForMeaning(input, formattedLanguages);
            }
            else
            {
                prompt = CreatePromptForWord(input, formattedLanguages);
            }

            return prompt;
        }

        #endregion

        #region Private Methods

        private string CreatePromptForMeaning(TranslationInput input, string formattedLanguages)
        {
            var prompt = $"Translate '{input.Meaning}' from the language '{input.SourceLanguage}' into the languages {formattedLanguages}. ";

            if (input.Examples?.Count() > 0)
            {
                string examplesFlat = "'" + string.Join("', '", input.Examples) + "'";
                prompt += $"Check also examples to get a better context: {examplesFlat}.";
            }

            return prompt;
        }

        private static string CreatePromptForWord(TranslationInput input, string formattedLanguages)
        {
            string partOfSpeechPlaceholder = !string.IsNullOrEmpty(input.PartOfSpeech) ? $", where the part of speech is: '{input.PartOfSpeech}'" : "";
            string meaningPlaceholder = !string.IsNullOrEmpty(input.Meaning) ? $"The word, in this context, means: '{input.Meaning}'. " : "";

            var prompt = $"Translate the word '{input.Word}' from the language '{input.SourceLanguage}' into the languages {formattedLanguages}{partOfSpeechPlaceholder}. "
                + $"{meaningPlaceholder}Provide between 1 and 3 possible answers so I can choose the best one. ";

            if (input.Examples?.Count() > 0)
            {
                string examplesFlat = "'" + string.Join("', '", input.Examples) + "'";
                prompt += $"Check also examples to get a better context: {examplesFlat}. ";
            }

            if (!string.IsNullOrEmpty(input.PartOfSpeech))
            {
                prompt += "Maintain the same part of speech in the translations. ";
            }

            prompt += "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.";
            return prompt;
        }

        private static ChatCompletionOptions CreateChatCompletionOptions()
        {
            JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerOptions.Default)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };

            JsonSchemaExporterOptions exporterOptions = new()
            {
                TreatNullObliviousAsNonNullable = true,
            };

            JsonNode schema = jsonSerializerOptions.GetJsonSchemaAsNode(typeof(TranslationOutput), exporterOptions);

            BinaryData jsonSchema = BinaryData.FromString(schema.ToString());

            ChatCompletionOptions options = new()
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
