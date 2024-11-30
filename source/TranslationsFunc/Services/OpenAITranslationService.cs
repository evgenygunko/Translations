using TranslationsFunc.Models;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace TranslationsFunc.Services
{
    public interface IOpenAITranslationService : ITranslationService
    {
    }

    public class OpenAITranslationService : IOpenAITranslationService
    {
        public async Task<TranslationOutput> TranslateAsync(TranslationInput input)
        {
            string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;

            var prompt = CreatePrompt(input);

            List<ChatMessage> messages = [new UserChatMessage(prompt)];
            ChatCompletionOptions options = CreateChatCompletionOptions();

            ChatClient client = new(model: "gpt-4o-mini", apiKey: key);
            ChatCompletion completion = await client.CompleteChatAsync(messages, options);

            string json = completion.Content[0].Text;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var translationOutput = JsonSerializer.Deserialize<TranslationOutput>(json, jsonOptions);

            return translationOutput ?? new TranslationOutput(Array.Empty<TranslationItem>());
        }

        internal string CreatePrompt(TranslationInput input)
        {
            string formattedLanguages = string.Join(", ", input.DestinationLanguages.Select(lang => $"'{lang}'"));

            string meaningPlaceholder = !string.IsNullOrEmpty(input.PartOfSpeech) ? $", where the part of speech is: '{input.PartOfSpeech}'" : "";

            var prompt = $"Translate the word '{input.Word}' from the language '{input.SourceLanguage}' into the languages {formattedLanguages}{meaningPlaceholder}. "
                + $"The word, in this context, means: '{input.Meaning}'. Provide between 1 and 3 possible answers so I can choose the best one. ";

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
    }
}
