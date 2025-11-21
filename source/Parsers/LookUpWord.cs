// Ignore Spelling: Downloader Dict ddo

using System.Text;
using System.Web;
using CopyWords.Parsers.Models;
using CopyWords.Parsers.Models.DDO;
using CopyWords.Parsers.Services;

namespace CopyWords.Parsers
{
    public interface ILookUpWord
    {
        Task<WordModel?> LookUpWordAsync(string searchTerm, string language, CancellationToken cancellationToken);
    }

    public class LookUpWord : ILookUpWord
    {
        private readonly IDDOPageParser _ddoPageParser;
        private readonly ISpanishDictPageParser _spanishDictPageParser;
        private readonly IFileDownloader _fileDownloader;

        public LookUpWord(
            IDDOPageParser ddoPageParser,
            ISpanishDictPageParser spanishDictPageParser,
            IFileDownloader fileDownloader)
        {
            _ddoPageParser = ddoPageParser;
            _spanishDictPageParser = spanishDictPageParser;
            _fileDownloader = fileDownloader;
        }

        #region Public Methods

        public async Task<WordModel?> LookUpWordAsync(string searchTerm, string language, CancellationToken cancellationToken)
        {
            string url;
            if (string.Equals(language, SourceLanguage.Danish.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Danish dictionary has "word variants", which have direct URLs. If a user clicks on such a link, we need to use it directly.
                if (searchTerm.StartsWith(DDOPageParser.DDOBaseUrl, StringComparison.CurrentCultureIgnoreCase))
                {
                    url = searchTerm;
                }
                else
                {
                    string encodedSearchTerm = HttpUtility.UrlEncode(searchTerm);
                    url = DDOPageParser.DDOBaseUrl + $"?query={encodedSearchTerm}";
                }
            }
            else
            {
                string encodedSearchTerm = HttpUtility.UrlEncode(searchTerm);
                url = SpanishDictPageParser.SpanishDictBaseUrl + encodedSearchTerm;
            }

            var wordModel = await GetWordByUrlAsync(url, language, cancellationToken);
            return wordModel;
        }

        #endregion

        #region Internal Methods

        internal async Task<WordModel?> GetWordByUrlAsync(string url, string language, CancellationToken cancellationToken)
        {
            // Download and parse a page from the online dictionary
            string? html = await _fileDownloader.DownloadPageAsync(url, Encoding.UTF8, cancellationToken);
            if (string.IsNullOrEmpty(html))
            {
                return null;
            }

            SourceLanguage sourceLanguage = Enum.Parse<SourceLanguage>(language);

            WordModel? wordModel;
            switch (sourceLanguage)
            {
                case SourceLanguage.Danish:
                    wordModel = ParseDanishWord(html);
                    break;

                case SourceLanguage.Spanish:
                    wordModel = ParseSpanishWord(html);
                    break;

                default:
                    throw new ArgumentException($"Source language '{sourceLanguage}' is not supported");
            }

            return wordModel;
        }

        internal WordModel ParseDanishWord(string html)
        {
            // Download and parse a page from DDO
            _ddoPageParser.LoadHtml(html);
            string headWordDA = _ddoPageParser.ParseHeadword();

            string partOfSpeech = _ddoPageParser.ParsePartOfSpeech();
            string endings = _ddoPageParser.ParseEndings();

            string soundUrl = _ddoPageParser.ParseSound();
            string soundFileName = string.IsNullOrEmpty(soundUrl) ? string.Empty : $"{headWordDA}.mp3";

            List<DDODefinition> ddoDefinitions = _ddoPageParser.ParseDefinitions();

            // For DDO, we create one Definition with one Context and several Meanings.
            List<Meaning> meanings = new List<Meaning>();
            int pos = 1;
            foreach (var ddoDefinition in ddoDefinitions)
            {
                meanings.Add(new Meaning(Original: ddoDefinition.Meaning, Translation: null, AlphabeticalPosition: (pos++).ToString(), ddoDefinition.Tag, ImageUrl: null, Examples: ddoDefinition.Examples));
            }

            Context context = new Context(ContextEN: "", Position: "", meanings);
            Definition definition = new Definition(
                Headword: new Headword(Original: headWordDA, English: null, Russian: null),
                PartOfSpeech: partOfSpeech,
                Endings: endings,
                Contexts: [context]);

            var wordModel = new WordModel(
                Word: headWordDA,
                SourceLanguage: SourceLanguage.Danish,
                SoundUrl: soundUrl,
                SoundFileName: soundFileName,
                Definitions: [definition],
                Variations: _ddoPageParser.ParseVariants()
            );

            return wordModel;
        }

        internal WordModel? ParseSpanishWord(string html)
        {
            Models.SpanishDict.WordJsonModel? wordObj = _spanishDictPageParser.ParseWordJson(html);
            if (wordObj == null)
            {
                return null;
            }

            string headwordES = _spanishDictPageParser.ParseHeadword(wordObj);

            string? soundUrl = _spanishDictPageParser.ParseSoundURL(wordObj);
            string? soundFileName = null;

            if (!string.IsNullOrEmpty(soundUrl))
            {
                soundFileName = $"{headwordES}.mp4";
            }

            // SpanishDict can return several definitions (e.g. for a "transitive verb" and "reflexive verb").
            IEnumerable<Models.SpanishDict.SpanishDictDefinition> spanishDictDefinitions = _spanishDictPageParser.ParseDefinitions(wordObj);

            List<Definition> definitions = new();
            foreach (var spanishDictDefinition in spanishDictDefinitions)
            {
                List<Context> contexts = new();
                foreach (var spanishDictContext in spanishDictDefinition.Contexts)
                {
                    // We don't want to translate meanings for Spanish words. They usually are very short and consist of one word.
                    IEnumerable<Meaning> meanings = spanishDictContext.Meanings.Select(
                        x => new Meaning(Original: x.Original, Translation: null, AlphabeticalPosition: x.AlphabeticalPosition, Tag: null, ImageUrl: x.ImageUrl, Examples: x.Examples));
                    contexts.Add(new Context(spanishDictContext.ContextEN, spanishDictContext.Position.ToString(), meanings));
                }

                // Spanish words don't have endings, this property only makes sense for Danish
                definitions.Add(new Definition(
                    Headword: new Headword(Original: spanishDictDefinition.WordES, English: null, Russian: null),
                    PartOfSpeech: spanishDictDefinition.PartOfSpeech,
                    Endings: "",
                    Contexts: contexts));
            }

            var wordModel = new WordModel(
                Word: headwordES,
                SourceLanguage: SourceLanguage.Spanish,
                SoundUrl: soundUrl,
                SoundFileName: soundFileName,
                Definitions: definitions,
                Variations: [] // there are no word variants in SpanishDict
            );

            return wordModel;
        }

        #endregion
    }
}
