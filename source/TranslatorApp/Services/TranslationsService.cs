// Ignore Spelling: App

using CopyWords.Parsers.Models;
using TranslatorApp.Models;

namespace TranslatorApp.Services
{
    public interface ITranslationsService
    {
        Task<WordModel> TranslateAsync(WordModel wordModel);

        (bool hasLanguageSpecificCharacters, string language) CheckLanguageSpecificCharacters(string text);
    }

    public class TranslationsService : ITranslationsService
    {
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IOpenAITranslationService2 _openAITranslationService2;
        private readonly ILogger<TranslationsService> _logger;

        public TranslationsService(
            IOpenAITranslationService openAITranslationService,
            IOpenAITranslationService2 openAITranslationService2,
            ILogger<TranslationsService> logger)
        {
            _openAITranslationService = openAITranslationService;
            _openAITranslationService2 = openAITranslationService2;
            _logger = logger;
        }

        #region Public Methods

        public async Task<WordModel> TranslateAsync(WordModel wordModel)
        {
            Models.Translation.TranslationInput translationInput = CreateTranslationInputFromWordModel(wordModel);

            // Check environment variable to determine which translation service to use
            var key = Environment.GetEnvironmentVariable("USE_OPENAI_RESPONSE_API")
                ?? Environment.GetEnvironmentVariable("USE_OPENAI_RESPONSE_API", EnvironmentVariableTarget.User);

            bool shouldUseResponseAPI = !string.IsNullOrEmpty(key) &&
                (key.Equals("true", StringComparison.OrdinalIgnoreCase) || key.Equals("1", StringComparison.OrdinalIgnoreCase));

            Models.Translation.TranslationOutput? translationOutput;
            if (shouldUseResponseAPI)
            {
                translationOutput = await _openAITranslationService2.TranslateAsync(translationInput);
            }
            else
            {
                translationOutput = await _openAITranslationService.TranslateAsync(translationInput);
            }

            if (translationOutput == null)
            {
                return wordModel;
            }

            WordModel wordModelWithTranslations = CreateWordModelFromTranslationOutput(wordModel, translationOutput);
            return wordModelWithTranslations;
        }

        public (bool hasLanguageSpecificCharacters, string language) CheckLanguageSpecificCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (false, string.Empty);
            }

            // Check for Danish characters
            var danishCharacters = new HashSet<char> { 'æ', 'ø', 'å', 'Æ', 'Ø', 'Å' };
            if (text.Any(danishCharacters.Contains))
            {
                return (true, SourceLanguage.Danish.ToString());
            }

            // Check for Spanish characters
            var spanishCharacters = new HashSet<char> { 'ñ', 'Ñ', 'í', 'Í', 'á', 'Á', 'é', 'É', 'ó', 'Ó', 'ú', 'Ú', 'ü', 'Ü' };
            if (text.Any(spanishCharacters.Contains))
            {
                return (true, SourceLanguage.Spanish.ToString());
            }

            return (false, string.Empty);
        }

        #endregion

        #region Internal Methods

        internal Models.Translation.TranslationInput CreateTranslationInputFromWordModel(WordModel wordModel)
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
                SourceLanguage: wordModel.SourceLanguage.ToString(),
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
                    int contextId = contextsWithTranslations.Count + 1;
                    Models.Translation.ContextOutput? translationContext = translationDefinition.Contexts.FirstOrDefault(d => d.id == contextId);
                    if (translationContext == null)
                    {
                        // Sometimes OpenAI returns null context (when the input doesn't have any "meanings" for a Danish word).
                        // It is random, it might return a context or not.
                        translationContext = new Models.Translation.ContextOutput(id: contextId, Meanings: []);

                        string contextIds = string.Join(", ", translationDefinition.Contexts.Select(c => c.id));
                        _logger.LogWarning(new EventId((int)TranslatorAppEventId.OpenAPIDidNotReturnContext),
                            "OpenAPI did not return a context. Trying to find a context with id '{ContextId}', but the returned object has '{AvailableContexts}' contexts with ids '{ReturnedContextIds}'.",
                            contextId,
                            translationDefinition.Contexts.Count(),
                            contextIds);
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
                SourceLanguage: wordModel.SourceLanguage,
                SoundUrl: wordModel.SoundUrl,
                SoundFileName: wordModel.SoundFileName,
                Definitions: definitionsWithTranslations,
                Variations: wordModel.Variations);
            return wordModelWithTranslations;
        }

        #endregion

        #region Private Methods

        private Headword CreateHeadWordWithTranslations(Headword headwordOriginal, Models.Translation.DefinitionOutput outputDefinition)
        {
            return new Headword(
                Original: headwordOriginal.Original,
                English: outputDefinition.HeadwordTranslationEnglish,
                Russian: outputDefinition.HeadwordTranslation);
        }

        #endregion
    }
}
