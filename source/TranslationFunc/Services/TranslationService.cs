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

            // Translate headwords in one request
            var options = new TextTranslationTranslateOptions(
                sourceLanguage: translationInput.SourceLanguage,
                targetLanguages: translationInput.DestinationLanguages,
                content: new[] { translationInput.HeadWord });
            var response = await client.TranslateAsync(options).ConfigureAwait(false);
            IReadOnlyList<TranslatedTextItem> translatedHeadWords = response.Value;

            IReadOnlyList<TranslatedTextItem> translatedMeanings;
            if (translationInput.Meanings.Any())
            {
                // Translate meanings in one request
                options = new TextTranslationTranslateOptions(
                    sourceLanguage: translationInput.SourceLanguage,
                    targetLanguages: translationInput.DestinationLanguages,
                    content: translationInput.Meanings);

                response = await client.TranslateAsync(options).ConfigureAwait(false);
                translatedMeanings = response.Value;
            }
            else
            {
                translatedMeanings = new List<TranslatedTextItem>().AsReadOnly();
            }

            // Create response
            foreach (string destinationLanguage in translationInput.DestinationLanguages)
            {
                var translatedHeadWordTextItems = translatedHeadWords.Select(x => x.Translations.FirstOrDefault(y => y.TargetLanguage == destinationLanguage));
                string? translatedHeadWord = translatedHeadWordTextItems.FirstOrDefault()?.Text;

                var translatedMeaningsTextItems = translatedMeanings.Select(x => x.Translations.FirstOrDefault(y => y.TargetLanguage == destinationLanguage));
                translations.Add(new TranslationOutput(destinationLanguage, translatedHeadWord, translatedMeaningsTextItems.Select(x => x?.Text)));
            }

            return translations;
        }
    }
}
