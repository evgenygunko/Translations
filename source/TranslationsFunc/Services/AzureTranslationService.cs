using Azure.AI.Translation.Text;
using Azure;
using TranslationsFunc.Models;

namespace TranslationsFunc.Services
{
    public interface IAzureTranslationService : ITranslationService
    {
    }

    public class AzureTranslationService : IAzureTranslationService
    {
        public async Task<TranslationOutput> TranslateAsync(TranslationInput translationInput)
        {
            string key = Environment.GetEnvironmentVariable("TRANSLATIONS_APP_KEY")!;
            string region = Environment.GetEnvironmentVariable("TRANSLATIONS_APP_REGION")!;

            AzureKeyCredential credential = new(key);
            TextTranslationClient client = new(credential, region);

            var options = new TextTranslationTranslateOptions(
                sourceLanguage: translationInput.SourceLanguage,
                targetLanguages: translationInput.DestinationLanguages,
                content: [translationInput.Word]);

            var response = await client.TranslateAsync(options).ConfigureAwait(false);
            IReadOnlyList<TranslatedTextItem> translatedWords = response.Value;

            // Create response
            List<TranslationItem> translationItems = new();
            foreach (string destinationLanguage in translationInput.DestinationLanguages)
            {
                var translatedWordTextItems = translatedWords.Select(x => x.Translations.FirstOrDefault(y => y.TargetLanguage == destinationLanguage));
                string? translatedWord = translatedWordTextItems.FirstOrDefault()?.Text;
                var translationVariants = string.IsNullOrEmpty(translatedWord) ? Enumerable.Empty<string>() : [translatedWord];

                translationItems.Add(new TranslationItem(destinationLanguage, translationVariants));
            }

            var translationOutput = new TranslationOutput(translationItems.ToArray());
            return translationOutput;
        }
    }
}
