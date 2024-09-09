using Azure.AI.Translation.Text;
using Azure;
using TranslationsFunc.Models;

namespace TranslationsFunc.Services
{
    public interface ITranslationService
    {
        Task<List<TranslationOutput>> TranslateAsync(TranslationInput translationInput);
    }

    public class TranslationService : ITranslationService
    {
        public async Task<List<TranslationOutput>> TranslateAsync(TranslationInput translationInput)
        {
            string key = Environment.GetEnvironmentVariable("TRANSLATIONS_APP_KEY")!;
            string region = Environment.GetEnvironmentVariable("TRANSLATIONS_APP_REGION")!;

            AzureKeyCredential credential = new(key);
            TextTranslationClient client = new(credential, region);
            var translations = new List<TranslationOutput>();

            foreach (string destinationLanguage in translationInput.DestinationLanguages)
            {
                string? translatedHeadWord = await TranslateTextAsync(client, translationInput.HeadWord, translationInput.SourceLanguage, destinationLanguage);

                List<string?> translatedMeanings = new List<string?>();
                foreach (string meaning in translationInput.Meanings)
                {
                    string? translatedMeaning = await TranslateTextAsync(client, meaning, translationInput.SourceLanguage, destinationLanguage);
                    translatedMeanings.Add(translatedMeaning);
                }

                translations.Add(new TranslationOutput(destinationLanguage, translatedHeadWord, translatedMeanings));
            }

            return translations;
        }

        private async Task<string?> TranslateTextAsync(TextTranslationClient client, string inputText, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrEmpty(inputText) || string.IsNullOrEmpty(sourceLanguage) || string.IsNullOrEmpty(targetLanguage))
            {
                return null;
            }

            Response<IReadOnlyList<TranslatedTextItem>> response = await client.TranslateAsync(targetLanguage, inputText, sourceLanguage).ConfigureAwait(false);
            IReadOnlyList<TranslatedTextItem> translations = response.Value;
            TranslatedTextItem? translation = translations.FirstOrDefault();

            return translation?.Translations?.FirstOrDefault()?.Text;
        }
    }
}
