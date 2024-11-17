using TranslationsFunc.Models;
using OpenAI.Chat;

namespace TranslationsFunc.Services
{
    public interface IOpenAITranslationService : ITranslationService
    {
    }

    public class OpenAITranslationService : IOpenAITranslationService
    {
        public async Task<List<TranslationOutput>> TranslateAsync(TranslationInput translationInput)
        {
            string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;

            var prompt = $"Translate the Danish word '{translationInput.HeadWord}' into Russian and English. The word in this context means: '{translationInput.Meanings.FirstOrDefault()}'. " +
                "Part of the speech: '{translationInput.PartsOfSpeech}'. " +
                "Return only translations as a json object and don't add any formatting. Example of the return object: '{ \"Russian\": \"необъяснимый\", \"English\": \"incomprehensible\" }'";

            ChatClient client = new(model: "gpt-4o-mini", apiKey: key);
            ChatCompletion completion = await client.CompleteChatAsync(prompt);

            string json = completion.Content[0].Text;
            List<TranslationOutput> translations = new()
            {
                new TranslationOutput("ru", "конечность", Enumerable.Empty<string>()),
                new TranslationOutput("en", "lim", Enumerable.Empty<string>()),
            };

            return translations;
        }
    }
}
