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
        public async Task<List<TranslationOutput>> TranslateAsync(TranslationInput input)
        {
            string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;

            string formattedLanguages = string.Join(", ", input.DestinationLanguages.Select(lang => $"'{lang}'"));

            var prompt = $"Translate the word '{input.Word}'' from the language '{input.SourceLanguage}' into the languages {formattedLanguages}. " +
                $"The word, in this context, means: '{input.Meaning}'. " +
                $"Part of speech: '{input.PartOfSpeech}'. " +
                "Return only translations as a JSON object, without any additional formatting. Example of the return object: '[ { \"Language\": \"ru\", \"Translation\": \"необъяснимый\" }, { \"Language\": \"en\", \"Translation\": \"incomprehensible\" } ]'";

            ChatClient client = new(model: "gpt-4o-mini", apiKey: key);
            ChatCompletion completion = await client.CompleteChatAsync(prompt);

            string json = completion.Content[0].Text;

            var translations = JsonSerializer.Deserialize<List<TranslationOutput>>(json);
            return translations ?? new List<TranslationOutput>();
        }
    }
}
