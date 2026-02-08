// Ignore Spelling: App

using CopyWords.Parsers;
using CopyWords.Parsers.Models;
using TranslatorApp.Models;

namespace TranslatorApp.Services
{
    public interface ITranslationsService
    {
        Task<WordModel?> LookUpWordInDictionaryAsync(string searchTerm, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default);

        Task<WordModel> TranslateAsync(WordModel wordModel, string sourceLanguage, string destinationLanguage, CancellationToken cancellationToken = default);
    }

    public class TranslationsService : ITranslationsService
    {
        private readonly IOpenAITranslationService _openAITranslationService;
        private readonly IOpenAITranslationService2 _openAITranslationService2;
        private readonly ILogger<TranslationsService> _logger;
        private readonly ILookUpWord _lookUpWord;
        private readonly ILaunchDarklyService _launchDarklyService;

        public TranslationsService(
            IOpenAITranslationService openAITranslationService,
            IOpenAITranslationService2 openAITranslationService2,
            ILogger<TranslationsService> logger,
            ILookUpWord lookUpWord,
            ILaunchDarklyService launchDarklyService)
        {
            _openAITranslationService = openAITranslationService;
            _openAITranslationService2 = openAITranslationService2;
            _logger = logger;
            _lookUpWord = lookUpWord;
            _launchDarklyService = launchDarklyService;
        }

        #region Public Methods

        public async Task<WordModel?> LookUpWordInDictionaryAsync(string searchTerm, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
        {
            // First check if the text has language specific characters - then use that language as source language
            if (CheckLanguageSpecificCharacters(searchTerm) is (true, string lang))
            {
                sourceLanguage = lang;
                _logger.LogInformation(new EventId((int)TranslatorAppEventId.LanguageSpecificCharactersFound),
                    "The text '{Text}' has language specific characters, will use '{Language}' as source language.",
                    searchTerm,
                    sourceLanguage);
            }

            _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupRequestReceived),
                "Will lookup '{Text}' in the '{SourceLanguage}' dictionary.",
                searchTerm,
                sourceLanguage);

            WordModel? wordModel = await _lookUpWord.LookUpWordAsync(searchTerm, sourceLanguage, cancellationToken);

            // If the source language is Danish and the word starts with "at ", remove "at " and search again.
            if (wordModel == null
                && string.Equals(sourceLanguage, SourceLanguage.Danish.ToString(), StringComparison.InvariantCultureIgnoreCase)
                && searchTerm.StartsWith("at ", StringComparison.InvariantCultureIgnoreCase))
            {
                string textWithoutAt = searchTerm[3..];

                _logger.LogInformation(new EventId((int)TranslatorAppEventId.RemoveAtPrefix),
                    "The text '{Text}' starts with 'at ' and the destination language is '{SourceLanguage}', so it is most likely a verb. " +
                    "DDO returns 'not found' when search with 'at ', will try again searching for '{TextWithoutAt}'.",
                    searchTerm,
                    sourceLanguage,
                    textWithoutAt);

                wordModel = await _lookUpWord.LookUpWordAsync(textWithoutAt, sourceLanguage, cancellationToken);
            }

            if (wordModel == null
                && !searchTerm.StartsWith(DDOPageParser.DDOBaseUrl, StringComparison.CurrentCultureIgnoreCase)
                && !searchTerm.StartsWith(SpanishDictPageParser.SpanishDictBaseUrl, StringComparison.CurrentCultureIgnoreCase))
            {
                // Try another parser - assuming that the user forgot to change the dictionary in the UI
                string anotherLanguage = string.Equals(sourceLanguage, SourceLanguage.Danish.ToString(), StringComparison.InvariantCultureIgnoreCase)
                    ? SourceLanguage.Spanish.ToString() : SourceLanguage.Danish.ToString();

                _logger.LogInformation(new EventId((int)TranslatorAppEventId.LookupRequestReceived),
                    "Word '{Text}' not found in the '{Dictionary}' dictionary. Will look it up in the '{AnotherDictionary}' dictionary.",
                    searchTerm,
                    sourceLanguage,
                    anotherLanguage);

                wordModel = await _lookUpWord.LookUpWordAsync(searchTerm, anotherLanguage, cancellationToken);
            }

            return wordModel;
        }

        public async Task<WordModel> TranslateAsync(WordModel wordModel, string sourceLanguage, string destinationLanguage, CancellationToken cancellationToken = default)
        {
            Models.Translation.TranslationInput translationInput = CreateTranslationInputFromWordModel(wordModel, sourceLanguage, destinationLanguage);

            Models.Translation.TranslationOutput? translationOutput;

            if (_launchDarklyService.GetBooleanFlag("use-open-ai-chat-completion"))
            {
                translationOutput = await _openAITranslationService.TranslateAsync(translationInput, cancellationToken);
            }
            else
            {
                translationOutput = await _openAITranslationService2.TranslateAsync(translationInput, cancellationToken);
            }

            if (translationOutput == null)
            {
                return wordModel;
            }

            WordModel wordModelWithTranslations = CreateWordModelFromTranslationOutput(wordModel, translationOutput);
            return wordModelWithTranslations;
        }

        #endregion

        #region Internal Methods
        internal (bool hasLanguageSpecificCharacters, string language) CheckLanguageSpecificCharacters(string text)
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

        internal Models.Translation.TranslationInput CreateTranslationInputFromWordModel(WordModel wordModel, string sourceLanguage, string destinationLanguage)
        {
            var definition = wordModel.Definition;
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

            var inputDefinition = new Models.Translation.DefinitionInput(
                Headword: headwordToTranslate,
                Contexts: contextsToTranslate);

            var translationInput = new Models.Translation.TranslationInput(
                Version: "2",
                SourceLanguage: sourceLanguage,
                DestinationLanguage: destinationLanguage,
                Definition: inputDefinition);

            return translationInput;
        }

        internal WordModel CreateWordModelFromTranslationOutput(WordModel wordModel, Models.Translation.TranslationOutput translationOutput)
        {
            var originalDefinition = wordModel.Definition;
            Models.Translation.DefinitionOutput translationDefinition = translationOutput.Definition;

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
                        LookupUrl: originalMeaning.LookupUrl,
                        Examples: originalMeaning.Examples));
                }

                contextsWithTranslations.Add(new Context(
                    ContextEN: originalContext.ContextEN,
                    Position: originalContext.Position,
                    Meanings: meaningsWithTranslations));
            }

            Definition definitionWithTranslations = new Definition(
                Headword: CreateHeadWordWithTranslations(originalDefinition.Headword, translationDefinition),
                PartOfSpeech: originalDefinition.PartOfSpeech,
                Endings: originalDefinition.Endings,
                Contexts: contextsWithTranslations);

            WordModel wordModelWithTranslations = new WordModel(
                Word: wordModel.Word,
                SourceLanguage: wordModel.SourceLanguage,
                SoundUrl: wordModel.SoundUrl,
                SoundFileName: wordModel.SoundFileName,
                Definition: definitionWithTranslations,
                Variants: wordModel.Variants,
                Expressions: wordModel.Expressions);

            return wordModelWithTranslations;
        }

        #endregion

        #region Private Methods

        private Headword CreateHeadWordWithTranslations(Headword headwordOriginal, Models.Translation.DefinitionOutput outputDefinition)
        {
            return new Headword(
                Original: headwordOriginal.Original,
                English: outputDefinition.HeadwordTranslationEnglish,
                Translation: outputDefinition.HeadwordTranslation,
                Russian: outputDefinition.HeadwordTranslation);
        }

        #endregion
    }
}
