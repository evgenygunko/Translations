using Azure.AI.Translation.Text;
using Azure;
using TranslationsFunc.Models.Output;
using TranslationsFunc.Models.Input;

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
            TextTranslationTranslateOptions options = CreateTranslationOptions(translationInput);

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

        internal static TextTranslationTranslateOptions CreateTranslationOptions(TranslationInput input)
        {
            string content;
            if (string.IsNullOrEmpty(input.Word) && !string.IsNullOrEmpty(input.Meaning))
            {
                content = input.Meaning;
            }
            else
            {
                content = input.Word;
            }

            return new TextTranslationTranslateOptions(
                sourceLanguage: input.SourceLanguage,
                targetLanguages: input.DestinationLanguages,
                content: [content]);
        }
    }
}
