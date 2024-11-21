using TranslationsFunc.Models;
using OpenAI.Chat;
using System.Text.Json;

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

            string formattedLanguages = string.Join(", ", input.DestinationLanguages.Select(lang => $"'{lang}'"));

            var prompt = $"Translate the word '{input.Word}' from the language '{input.SourceLanguage}' into the languages {formattedLanguages}, where the part of speech is: '{input.PartOfSpeech}'. " +
                $"The word, in this context, means: '{input.Meaning}'. ";

            if (input.Examples?.Count() > 0)
            {
                string examplesFlat = "'" + string.Join("', '", input.Examples) + "'";
                prompt += $"Check also examples to get a better context: {examplesFlat}. ";
            }

            prompt += "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.";

            List<ChatMessage> messages =
            [
                new UserChatMessage(prompt),
            ];

            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "translation_output",
                jsonSchema: BinaryData.FromBytes("""
                    {
                        "type": "object",
                        "properties": {
                        "translations": {
                            "type": "array",
                            "items": {
                            "type": "object",
                            "properties": {
                                "Language": { "type": "string" },
                                "Translation": { "type": "string" }
                            },
                            "required": ["Language", "Translation"],
                            "additionalProperties": false
                            }
                        }
                        },
                        "required": ["translations"],
                        "additionalProperties": false
                    }
                    """u8.ToArray()),
                jsonSchemaIsStrict: true)
            };

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
    }
}
