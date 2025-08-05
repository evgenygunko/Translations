// Ignore Spelling: App

using CopyWords.Parsers.Models;

namespace TranslatorApp.Services
{
    public interface ITranslationsService
    {
        Task<WordModel> TranslateAsync(string sourceLanguage, WordModel wordModel);
    }

    public class TranslationsService : ITranslationsService
    {
        private readonly IOpenAITranslationService _openAITranslationService;

        public TranslationsService(
            IOpenAITranslationService openAITranslationService)
        {
            _openAITranslationService = openAITranslationService;
        }

        public async Task<WordModel> TranslateAsync(string sourceLanguage, WordModel wordModel)
        {
            Models.Translation.TranslationInput translationInput = CreateTranslationInputFromWordModel(sourceLanguage, wordModel);

            Models.Translation.TranslationOutput? translationOutput = await _openAITranslationService.TranslateAsync(translationInput);

            if (translationOutput == null)
            {
                return wordModel;
            }

            WordModel wordModelWithTranslations = CreateWordModelFromTranslationOutput(wordModel, translationOutput);
            return wordModelWithTranslations;
        }

        internal Models.Translation.TranslationInput CreateTranslationInputFromWordModel(string sourceLanguage, WordModel wordModel)
        {
            var inputDefinitions = new List<Models.Translation.DefinitionInput>();

            foreach (var definition in wordModel.Definitions)
            {
                Meaning? firstMeaning = definition.Contexts.First().Meanings.FirstOrDefault();

                var headwordToTranslate = new Models.Translation.HeadwordInput(
                    Text: definition.Headword.Original,
                    PartOfSpeech: definition.PartOfSpeech,
                    Meaning: firstMeaning?.Original ?? "",
                    Examples: firstMeaning?.Examples?.Select(e => e.Original) ?? Enumerable.Empty<string>());

                var contextsToTranslate = new List<Models.Translation.ContextInput>();
                foreach (var context in definition.Contexts)
                {
                    var inputMeanings = new List<Models.Translation.MeaningInput>();

                    foreach (var meaning in context.Meanings)
                    {
                        inputMeanings.Add(new Models.Translation.MeaningInput(
                            id: inputMeanings.Count + 1,
                            Text: meaning.Original,
                            PartOfSpeech: definition.PartOfSpeech,
                            Examples: meaning.Examples.Select(e => e.Original)));
                    }

                    contextsToTranslate.Add(new Models.Translation.ContextInput(
                        id: contextsToTranslate.Count + 1,
                        ContextString: context.ContextEN,
                        Meanings: inputMeanings));
                }

                inputDefinitions.Add(new Models.Translation.DefinitionInput(
                    id: inputDefinitions.Count + 1,
                    Headword: headwordToTranslate,
                    Contexts: contextsToTranslate));
            }

            var translationInput = new Models.Translation.TranslationInput(
                Version: "2",
                SourceLanguage: sourceLanguage.ToString(),
                DestinationLanguage: "Russian",
                Definitions: inputDefinitions);

            return translationInput;
        }

        internal WordModel CreateWordModelFromTranslationOutput(WordModel wordModel, Models.Translation.TranslationOutput translationOutput)
        {
            var definitionsWithTranslations = new List<Definition>();

            foreach (var originalDefinition in wordModel.Definitions)
            {
                Models.Translation.DefinitionOutput translationDefinition = translationOutput.Definitions.First(d => d.id == definitionsWithTranslations.Count + 1);

                var contextsWithTranslations = new List<Context>();

                foreach (var originalContext in originalDefinition.Contexts)
                {
                    Models.Translation.ContextOutput translationContext;
                    if (translationDefinition.Contexts.Count() == 0)
                    {
                        // Sometimes OpenAI returns null context (when the input doesn't have any "meanings" for a Danish word). It is random, it might return a context or not.
                        translationContext = new Models.Translation.ContextOutput(
                            id: contextsWithTranslations.Count + 1,
                            Meanings: []);
                    }
                    else
                    {
                        translationContext = translationDefinition.Contexts.First(d => d.id == contextsWithTranslations.Count + 1);
                    }

                    var meaningsWithTranslations = new List<Meaning>();

                    foreach (var originalMeaning in originalContext.Meanings)
                    {
                        string? translationRU = translationContext.Meanings.FirstOrDefault(m => m.id == meaningsWithTranslations.Count + 1)?.MeaningTranslation;

                        meaningsWithTranslations.Add(new Meaning(
                            Original: originalMeaning.Original,
                            Translation: translationRU,
                            AlphabeticalPosition: originalMeaning.AlphabeticalPosition,
                            Tag: originalMeaning.Tag,
                            ImageUrl: originalMeaning.ImageUrl,
                            Examples: originalMeaning.Examples));
                    }

                    contextsWithTranslations.Add(new Context(
                        ContextEN: originalContext.ContextEN,
                        Position: originalContext.Position,
                        Meanings: meaningsWithTranslations));
                }

                definitionsWithTranslations.Add(new Definition(
                    Headword: CreateHeadWordWithTranslations(originalDefinition.Headword, translationDefinition),
                    PartOfSpeech: originalDefinition.PartOfSpeech,
                    Endings: originalDefinition.Endings,
                    Contexts: contextsWithTranslations));
            }

            WordModel wordModelWithTranslations = new WordModel(
                Word: wordModel.Word,
                SoundUrl: wordModel.SoundUrl,
                SoundFileName: wordModel.SoundFileName,
                Definitions: definitionsWithTranslations,
                Variations: wordModel.Variations);
            return wordModelWithTranslations;
        }

        private Headword CreateHeadWordWithTranslations(Headword headwordOriginal, Models.Translation.DefinitionOutput outputDefinition)
        {
            return new Headword(
                Original: headwordOriginal.Original,
                English: outputDefinition.HeadwordTranslationEnglish,
                Russian: outputDefinition.HeadwordTranslation);
        }
    }
}
