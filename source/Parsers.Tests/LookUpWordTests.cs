// Ignore Spelling: Dict Api Afeitar App

using System.Text;
using AutoFixture;
using CopyWords.Parsers.Models;
using CopyWords.Parsers.Services;
using FluentAssertions;
using Moq;

namespace CopyWords.Parsers.Tests
{
    [TestClass]
    public class LookUpWordTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _ = context;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        #region Tests for LookUpWordAsync

        [TestMethod]
        public async Task LookUpWordAsync_WhenSearchTermIsDDOUrl_CallsGivenUrl()
        {
            string searchTerm = "https://ordnet.dk/ddo/ordbog?select=bestemme&query=bestemt";
            SourceLanguage sourceLanguage = SourceLanguage.Danish;

            Mock<IDDOPageParser> ddoPageParserMock = _fixture.Freeze<Mock<IDDOPageParser>>();

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("bestemme.html");

            var sut = _fixture.Create<LookUpWord>();
            WordModel? result = await sut.LookUpWordAsync(searchTerm, sourceLanguage.ToString(), CancellationToken.None);

            result.Should().NotBeNull();
            ddoPageParserMock.Verify(x => x.ParseHeadword());
            fileDownloaderMock.Verify(x => x.DownloadPageAsync(searchTerm, Encoding.UTF8, It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenSearchTermIsSpanishDictUrl_CallsUrlWithoutQueryParameters()
        {
            string searchTerm = "https://www.spanishdict.com/translate/coche?n=1&p1";
            SourceLanguage sourceLanguage = SourceLanguage.Spanish;

            Mock<ISpanishDictPageParser> spanishDictPageParserMock = _fixture.Freeze<Mock<ISpanishDictPageParser>>();

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("coche.html");

            var sut = _fixture.Create<LookUpWord>();
            WordModel? result = await sut.LookUpWordAsync(searchTerm, sourceLanguage.ToString(), CancellationToken.None);

            result.Should().NotBeNull();
            spanishDictPageParserMock.Verify(x => x.ParseHeadword(It.IsAny<Models.SpanishDict.WordJsonModel>()));
            fileDownloaderMock.Verify(x => x.DownloadPageAsync("https://www.spanishdict.com/translate/coche", Encoding.UTF8, It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        [DataRow("haj")]
        [DataRow("")]
        [DataRow("æø")]
        [DataRow("snabel-a")]
        public async Task LookUpWordAsync_WhenSourceLanguageIsDanish_CallsDDOPageParser(string searchTerm)
        {
            SourceLanguage sourceLanguage = SourceLanguage.Danish;

            Mock<IDDOPageParser> ddoPageParserMock = _fixture.Freeze<Mock<IDDOPageParser>>();

            // Just return some valid HTML, so we get past the downloading part
            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("haj.html");

            var sut = _fixture.Create<LookUpWord>();
            WordModel? result = await sut.LookUpWordAsync(searchTerm, sourceLanguage.ToString(), CancellationToken.None);

            result.Should().NotBeNull();

            fileDownloaderMock.Verify(x => x.DownloadPageAsync(It.Is<string>(str => str.StartsWith("https://ordnet.dk/ddo/ordbog?query=")), Encoding.UTF8, It.IsAny<CancellationToken>()));
            ddoPageParserMock.Verify(x => x.ParseHeadword());
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenSourceLanguageIsSpanish_CallsSpanishDictPageParser()
        {
            string searchTerm = "ser";
            SourceLanguage sourceLanguage = SourceLanguage.Spanish;

            Mock<ISpanishDictPageParser> spanishDictPageParserMock = _fixture.Freeze<Mock<ISpanishDictPageParser>>();

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("ser.html");

            var sut = _fixture.Create<LookUpWord>();
            _ = sut.Invoking(y => y.LookUpWordAsync(searchTerm, sourceLanguage.ToString(), CancellationToken.None))
                .Should().ThrowAsync<ArgumentException>();

            WordModel? result = await sut.LookUpWordAsync(searchTerm, sourceLanguage.ToString(), CancellationToken.None);

            result.Should().NotBeNull();
            spanishDictPageParserMock.Verify(x => x.ParseHeadword(It.IsAny<Models.SpanishDict.WordJsonModel>()));
        }

        [TestMethod]
        public async Task LookUpWordAsync_Should_DownloadPageAndCallParser()
        {
            const string headWord = "haj";
            const string partOfSpeech = "substantiv, fælleskøn";
            const string endings = "-en, -er, -erne";
            const string soundUrl = "https://static.ordnet.dk/mp3/11019/11019539_1.mp3";

            var definition1 = new Models.DDO.DDODefinition("stor, langstrakt bruskfisk", Tag: null, Enumerable.Empty<Example>());
            var definition2 = new Models.DDO.DDODefinition("grisk, skrupelløs person", Tag: "slang", Enumerable.Empty<Example>());
            var definition3 = new Models.DDO.DDODefinition("person der er særlig dygtig til et spil", Tag: "slang", Enumerable.Empty<Example>());
            var definitions = new List<Models.DDO.DDODefinition>() { definition1, definition2, definition3 };

            var variants = new List<Variant>() { new Variant("haj", "https://ordnet.dk/ddo/ordbog?select=haj&query=haj") };

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("haj.html");

            Mock<IDDOPageParser> ddoPageParserMock = _fixture.Freeze<Mock<IDDOPageParser>>();
            ddoPageParserMock.Setup(x => x.ParseHeadword()).Returns(headWord);
            ddoPageParserMock.Setup(x => x.ParsePartOfSpeech()).Returns(partOfSpeech);
            ddoPageParserMock.Setup(x => x.ParseEndings()).Returns(endings);
            ddoPageParserMock.Setup(x => x.ParseSound()).Returns(soundUrl);
            ddoPageParserMock.Setup(x => x.ParseDefinitions()).Returns(definitions);
            ddoPageParserMock.Setup(x => x.ParseVariants()).Returns(variants);

            var sut = _fixture.Create<LookUpWord>();

            WordModel? result = await sut.LookUpWordAsync("haj", SourceLanguage.Danish.ToString(), CancellationToken.None);

            result.Should().NotBeNull();
            result!.Word.Should().Be(headWord);
            result!.SoundUrl.Should().Be(soundUrl);
            result!.SoundFileName.Should().Be("haj.mp3");

            // For DDO, we create one Definition with one Context and several Meanings.
            result!.Definitions.Should().HaveCount(1);
            var definition = result.Definitions.First();
            definition.Contexts.Should().HaveCount(1);
            var context = definition.Contexts.First();
            context.Meanings.Should().HaveCount(3);

            result!.Variations.Should().HaveCount(1);

            fileDownloaderMock.Verify(x => x.DownloadPageAsync(It.Is<string>(str => str.EndsWith($"?query={headWord}")), Encoding.UTF8, It.IsAny<CancellationToken>()));
        }

        #endregion

        #region Tests for GetWordByUrlAsync

        [TestMethod]
        public void GetWordByUrlAsync_WhenSourceLanguageIsNotDanish_ThrowsException()
        {
            string url = _fixture.Create<string>();

            var sut = _fixture.Create<LookUpWord>();

            _ = sut.Invoking(x => x.GetWordByUrlAsync(url, SourceLanguage.Spanish.ToString(), CancellationToken.None))
                .Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task GetWordByUrlAsync_Should_DownloadPageAndCallParser()
        {
            string url = _fixture.Create<string>();

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("haj.html");

            Mock<IDDOPageParser> ddoPageParserMock = _fixture.Freeze<Mock<IDDOPageParser>>();

            var sut = _fixture.Create<LookUpWord>();

            WordModel? result = await sut.GetWordByUrlAsync(url, SourceLanguage.Danish.ToString(), CancellationToken.None);

            result.Should().NotBeNull();

            fileDownloaderMock.Verify(x => x.DownloadPageAsync(url, Encoding.UTF8, It.IsAny<CancellationToken>()));

            ddoPageParserMock.Verify(x => x.LoadHtml(It.IsAny<string>()));
            ddoPageParserMock.Verify(x => x.ParseHeadword());
            ddoPageParserMock.Verify(x => x.ParseSound());
            ddoPageParserMock.Verify(x => x.ParseDefinitions());
            ddoPageParserMock.Verify(x => x.ParseVariants());
        }

        [TestMethod]
        public async Task GetWordByUrlAsync_WhenSoundUrlIsEmpty_SetsSoundFileNameToEmptyString()
        {
            string url = _fixture.Create<string>();
            string headWord = _fixture.Create<string>();

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("i forb. med.html");

            Mock<IDDOPageParser> ddoPageParserMock = _fixture.Freeze<Mock<IDDOPageParser>>();
            ddoPageParserMock.Setup(x => x.ParseHeadword()).Returns(headWord);
            ddoPageParserMock.Setup(x => x.ParseSound()).Returns(string.Empty);

            var sut = _fixture.Create<LookUpWord>();
            WordModel? result = await sut.GetWordByUrlAsync(url, SourceLanguage.Danish.ToString(), CancellationToken.None);

            result.Should().NotBeNull();
            result!.SoundFileName.Should().BeEmpty();
            result!.SoundUrl.Should().BeEmpty();

            fileDownloaderMock.Verify(x => x.DownloadPageAsync(url, Encoding.UTF8, It.IsAny<CancellationToken>()));

            ddoPageParserMock.Verify(x => x.ParseSound());
        }

        #endregion

        #region Tests for ParseDanishWord

        [TestMethod]
        public void ParseDanishWord_Should_ReturnOneDefinitionWithOneContextAndSeveralMeanings()
        {
            string html = _fixture.Create<string>();

            Mock<IFileDownloader> fileDownloaderMock = _fixture.Freeze<Mock<IFileDownloader>>();
            fileDownloaderMock.Setup(x => x.DownloadPageAsync(It.IsAny<string>(), Encoding.UTF8, It.IsAny<CancellationToken>())).ReturnsAsync("haj.html");

            Mock<IDDOPageParser> ddoPageParserMock = _fixture.Freeze<Mock<IDDOPageParser>>();
            ddoPageParserMock.Setup(x => x.ParseDefinitions()).Returns(CreateDefinitionsForHaj());

            var sut = _fixture.Create<LookUpWord>();

            WordModel result = sut.ParseDanishWord(html);

            result.Should().NotBeNull();

            result.SourceLanguage.Should().Be(SourceLanguage.Danish);

            IEnumerable<Definition> definitions = result!.Definitions;
            definitions.Should().HaveCount(1);

            // For DDO, we create one Definition with one Context and several Meanings.
            Definition definition1 = definitions.First();
            definition1.Contexts.Should().HaveCount(1);
            Context context1 = definition1.Contexts.First();
            context1.Meanings.Should().HaveCount(3);

            Meaning meaning1 = context1.Meanings.First();
            meaning1.Original.Should().Be("stor, langstrakt bruskfisk");
            meaning1.AlphabeticalPosition.Should().Be("1");
            meaning1.Tag.Should().BeNull();
            meaning1.ImageUrl.Should().BeNull();
            meaning1.Examples.Should().HaveCount(1);
            Example example1 = meaning1!.Examples.First();
            example1.Original.Should().Be("Hubertus [vidste], at det var en haj, der kredsede rundt og håbede på, at en sørøver skulle gå planken ud eller blive kølhalet, så den kunne æde ham");

            Meaning meaning2 = context1.Meanings.Skip(1).First();
            meaning2.Original.Should().Be("grisk, skrupelløs person der ved ulovlige eller ufine metoder opnår økonomisk gevinst på andres bekostning");
            meaning2.AlphabeticalPosition.Should().Be("2");
            meaning2.Tag.Should().Be("SLANG");
            meaning2.ImageUrl.Should().BeNull();
            meaning2.Examples.Should().HaveCount(1);
            Example example2 = meaning2!.Examples.First();
            example2.Original.Should().Be("-");

            Meaning meaning3 = context1.Meanings.Skip(2).First();
            meaning3.Original.Should().Be("person der er særlig dygtig til et spil, håndværk el.lign.");
            meaning3.AlphabeticalPosition.Should().Be("3");
            meaning3.Tag.Should().Be("SLANG");
            meaning3.ImageUrl.Should().BeNull();
            meaning3.Examples.Should().HaveCount(1);
            Example example3 = meaning3!.Examples.First();
            example3.Original.Should().Be("Chamonix er et \"must\" for dig, som er en haj på ski. Her finder du noget af alpernes \"tuffeste\" skiløb");

            ddoPageParserMock.Verify(x => x.LoadHtml(It.IsAny<string>()));
            ddoPageParserMock.Verify(x => x.ParseHeadword());
            ddoPageParserMock.Verify(x => x.ParseSound());
            ddoPageParserMock.Verify(x => x.ParseDefinitions());
            ddoPageParserMock.Verify(x => x.ParseVariants());
        }

        #endregion

        #region Tests for ParseSpanishWord

        [TestMethod]
        public void ParseSpanishWord_Should_Return2MeaningsForAfeitar()
        {
            string headwordES = "afeitar";
            string html = _fixture.Create<string>();

            Mock<ISpanishDictPageParser> spanishDictPageParserMock = _fixture.Freeze<Mock<ISpanishDictPageParser>>();
            spanishDictPageParserMock.Setup(x => x.ParseHeadword(It.IsAny<Models.SpanishDict.WordJsonModel>())).Returns(headwordES);
            spanishDictPageParserMock.Setup(x => x.ParseDefinitions(It.IsAny<Models.SpanishDict.WordJsonModel>())).Returns(CreateDefinitionsForAfeitar());
            spanishDictPageParserMock.Setup(x => x.ParseVariants(It.IsAny<Models.SpanishDict.WordJsonModel>())).Returns(
                new List<Variant>
                    {
                        new Variant("afeitar", _fixture.Create<Uri>().ToString()),
                        new Variant("afeitarse", _fixture.Create<Uri>().ToString())
                    });

            var sut = _fixture.Create<LookUpWord>();

            WordModel? result = sut.ParseSpanishWord(html, n: null, p: null);

            result.Should().NotBeNull();

            result!.Word.Should().Be(headwordES);
            result.SourceLanguage.Should().Be(SourceLanguage.Spanish);

            var variations = result!.Variations.ToList();
            variations.Should().HaveCount(2);
            variations[0].Word.Should().Be("afeitar");
            variations[1].Word.Should().Be("afeitarse");

            IEnumerable<Definition> definitions = result!.Definitions;
            definitions.Should().HaveCount(2);

            // 1. afeitar
            Definition definition1 = definitions.First();
            definition1.Headword.Original.Should().Be("afeitar");
            definition1.Contexts.Should().HaveCount(1);
            definition1.PartOfSpeech.Should().Be("TRANSITIVE VERB");
            Context context1 = definition1.Contexts.First();
            context1.ContextEN.Should().Be("(to remove hair)");
            context1.Position.Should().Be("1");
            context1.Meanings.Should().HaveCount(1);

            Meaning meaning1 = context1.Meanings.First();
            meaning1.Original.Should().Be("to shave");
            meaning1.AlphabeticalPosition.Should().Be("a");
            meaning1.Tag.Should().BeNull();
            meaning1.ImageUrl.Should().BeNull();
            meaning1.Examples.Should().HaveCount(1);
            Example example1 = meaning1!.Examples.First();
            example1.Original.Should().Be("Para el verano, papá decidió afeitar al perro.");
            example1.Translation.Should().Be("For the summer, dad decided to shave the dog.");

            // 2. afeitarse
            Definition definition2 = definitions.Skip(1).First();
            definition2.Headword.Original.Should().Be("afeitarse");
            definition2.PartOfSpeech.Should().Be("REFLEXIVE VERB");
            definition2.Contexts.Should().HaveCount(1);
            context1 = definition2.Contexts.First();
            context1.Position.Should().Be("1");
            context1.Meanings.Should().HaveCount(1);

            meaning1 = context1.Meanings.First();
            meaning1.Original.Should().Be("to shave");
            meaning1.AlphabeticalPosition.Should().Be("a");
            meaning1.Tag.Should().BeNull();
            meaning1.ImageUrl.Should().BeNull();
            meaning1.Examples.Should().HaveCount(1);
            example1 = meaning1!.Examples.First();
            example1.Original.Should().Be("¿Con qué frecuencia te afeitas la barba?");
            example1.Translation.Should().Be("How often do you shave your beard?");

            spanishDictPageParserMock.Verify(x => x.ParseWordJson(html));
            spanishDictPageParserMock.Verify(x => x.ParseHeadword(It.IsAny<Models.SpanishDict.WordJsonModel>()));
            spanishDictPageParserMock.Verify(x => x.ParseSoundURL(It.IsAny<Models.SpanishDict.WordJsonModel>()));
            spanishDictPageParserMock.Verify(x => x.ParseDefinitions(It.IsAny<Models.SpanishDict.WordJsonModel>()));
        }

        #endregion

        #region Private Methods

        private static List<Models.DDO.DDODefinition> CreateDefinitionsForHaj()
        {
            var definition1 = new Models.DDO.DDODefinition(Meaning: "stor, langstrakt bruskfisk", Tag: null, new List<Example>()
            {
                    new Example(Original: "Hubertus [vidste], at det var en haj, der kredsede rundt og håbede på, at en sørøver skulle gå planken ud eller blive kølhalet, så den kunne æde ham", Translation: null)
                });

            var definition2 = new Models.DDO.DDODefinition(Meaning: "grisk, skrupelløs person der ved ulovlige eller ufine metoder opnår økonomisk gevinst på andres bekostning", Tag: "SLANG", Examples: new List<Example>()
                {
                    new Example(Original: "-", Translation: null)
                });

            var definition3 = new Models.DDO.DDODefinition(Meaning: "person der er særlig dygtig til et spil, håndværk el.lign.", Tag: "SLANG", Examples: new List<Example>()
                {
                    new Example(Original : "Chamonix er et \"must\" for dig, som er en haj på ski. Her finder du noget af alpernes \"tuffeste\" skiløb", Translation: null)
                });

            return new List<Models.DDO.DDODefinition>() { definition1, definition2, definition3 };
        }

        private static List<Models.SpanishDict.SpanishDictDefinition> CreateDefinitionsForAfeitar()
        {
            var definition1 = new Models.SpanishDict.SpanishDictDefinition(WordES: "afeitar", PartOfSpeech: "TRANSITIVE VERB",
                new List<Models.SpanishDict.SpanishDictContext>
                {
                    new Models.SpanishDict.SpanishDictContext(ContextEN: "(to remove hair)", Position: 1,
                        new List<Models.SpanishDict.Meaning>
                        {
                            new Models.SpanishDict.Meaning(Original: "to shave", AlphabeticalPosition: "a", ImageUrl: null,
                                new List<Example>() { new Example(Original: "Para el verano, papá decidió afeitar al perro.", Translation: "For the summer, dad decided to shave the dog.") }),
                        }),
                });

            var definition2 = new Models.SpanishDict.SpanishDictDefinition(WordES: "afeitarse", PartOfSpeech: "REFLEXIVE VERB",
                new List<Models.SpanishDict.SpanishDictContext>
                {
                    new Models.SpanishDict.SpanishDictContext(ContextEN: "(to shave oneself)", Position: 1,
                        new List<Models.SpanishDict.Meaning>
                        {
                            new Models.SpanishDict.Meaning(Original: "to shave", AlphabeticalPosition: "a", ImageUrl: null,
                                new List<Example>() { new Example(Original: "¿Con qué frecuencia te afeitas la barba?", Translation: "How often do you shave your beard?") }),
                        }),
                });

            return new List<Models.SpanishDict.SpanishDictDefinition>() { definition1, definition2 };
        }

        #endregion
    }
}
