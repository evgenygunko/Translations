using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
using AutoFixture;
using FluentAssertions;
using Moq;
using OpenAI.Chat;
using TranslationFunc.Tests;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Models.Output;
using TranslationsFunc.Services;

namespace TranslationsFunc.Tests.Services
{
    [TestClass]
    public class OpenAITranslationServiceTests
    {
        private static string s_testDataPath = default!;

        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            s_testDataPath = Path.Combine(currentDir, "TestData");
        }

        #region Tests for TranslateAsync

        [TestMethod]
        public async Task TranslateAsync_Should_CallChatClient()
        {
            TranslationInput translationInput = _fixture.Create<TranslationInput>();

            ClientResult<ChatCompletion> headwordTranslationsResponse = CreateChatCompletionFromJson("OpenAIHeadwordTranslations.json");
            ClientResult<ChatCompletion> meaningsTranslationsResponse = CreateChatCompletionFromJson("OpenAIMeaningsTranslations.json");

            var chatClientMock = new Mock<ChatClient>();
            chatClientMock
                .SetupSequence(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(headwordTranslationsResponse)
                .ReturnsAsync(meaningsTranslationsResponse);

            var sut = new OpenAITranslationService(chatClientMock.Object);

            TranslationOutput result = await sut.TranslateAsync(translationInput);

            result.Should().NotBeNull();

            // Check translations for headword
            result.Headword.Should().HaveCount(2);

            Models.Output.Headword headword;

            headword = result.Headword.First();
            headword.Language.Should().Be("ru");
            headword.HeadwordTranslations.First().Should().Be("такие как");

            headword = result.Headword.Skip(1).First();
            headword.Language.Should().Be("en");
            headword.HeadwordTranslations.First().Should().Be("such as");

            // Check translations for meanings
            result.Meanings.Should().HaveCount(2);

            Models.Output.Meaning meaning;

            meaning = result.Meanings.First();
            meaning.id.Should().Be(1);
            meaning.MeaningTranslations.First().Language.Should().Be("ru");
            meaning.MeaningTranslations.First().Text.Should().Be("используется для указания одного или нескольких примеров чего-либо");

            meaning = result.Meanings.Skip(1).First();
            meaning.id.Should().Be(2);
            meaning.MeaningTranslations.Last().Language.Should().Be("en");
            meaning.MeaningTranslations.Last().Text.Should().Be("used as an introduction to a subordinate clause that indicates a reason");
        }

        #endregion

        #region Tests for Translate2Async

        [TestMethod]
        public async Task Translate2Async_Should_CallChatClientForEachDefinition()
        {
            // Input doesn't matter, but it must have 2 definitions, because we mock 2 responses
            var translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "es",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "noun",
                        Headword: new Headword2(Text: "word to translate", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts:[
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Models.Input.Meaning(id : 1, Text : "meaning 1", Examples :["meaning 1, example 1"]) ]
                            )
                        ]
                    ),
                    new Definition2(
                        id: 2,
                        PartOfSpeech: "noun",
                        Headword: new Headword2(Text: "word to translate", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Models.Input.Meaning(id : 1, Text : "meaning 1", Examples :["meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var openAIResponse = CreateChatCompletionFromJson("OpenAITranslationsAfeitar.json");

            var chatClientMock = new Mock<ChatClient>();
            chatClientMock
                .Setup(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(openAIResponse);

            var sut = new OpenAITranslationService(chatClientMock.Object);

            TranslationOutput2 result = await sut.Translate2Async(translationInput);

            result.Should().NotBeNull();

            result.Definitions.Should().HaveCount(2);

            DefinitionOutput2 definition;
            MeaningOutput2 meaning;

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            definition = result.Definitions[0];
            definition.id.Should().Be(1);

            // Check translations for headword
            definition.HeadwordTranslation.Should().Be("брить");

            // Check translations for meanings
            definition.Contexts.Should().HaveCount(1);
            definition.Contexts[0].Meanings.Should().HaveCount(1);

            meaning = definition.Contexts[0].Meanings[0];
            meaning.id.Should().Be(1);
            meaning.MeaningTranslation.Should().Be("брить (удалять волосы), сбрить, срезать");

            /***********************************************************************/
            // Afeitarse
            /***********************************************************************/
            definition = result.Definitions[1];
            definition.id.Should().Be(2);

            // Check translations for headword
            definition.HeadwordTranslation.Should().Be("бриться");

            // Check translations for meanings
            definition.Contexts.Should().HaveCount(1);
            definition.Contexts[0].Meanings.Should().HaveCount(1);

            meaning = definition.Contexts[0].Meanings[0];
            meaning.id.Should().Be(1);
            meaning.MeaningTranslation.Should().Be("брить (брить себя), самообритие, сбривать");
        }

        #endregion

        #region Tests for CreatePromptForHeadword

        [TestMethod]
        public void CreatePromptForHeadword_WhenPartOfSpeechIsNotEmpty_AddsItToPrompt()
        {
            const string partOfSpeech = "PartOfSpeech";

            string result = OpenAITranslationService.CreatePromptForHeadword(
                word: "Word",
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: "Meaning",
                partOfSpeech: partOfSpeech,
                examples: []);

            result.Should().Be(
                "Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'." + Environment.NewLine +
                "The word, in this context, means: 'Meaning'." + Environment.NewLine +
                "Provide between 1 and 3 possible answers so I can choose the best one." + Environment.NewLine +
                "Maintain the same part of speech in the translations." + Environment.NewLine +
                "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePromptForHeadword_WhenPartOfSpeechIsEmpty_DoesNotAddItToPrompt()
        {
            const string partOfSpeech = "";

            string result = OpenAITranslationService.CreatePromptForHeadword(
                word: "Word",
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: "Meaning",
                partOfSpeech: partOfSpeech,
                examples: []);

            result.Should().Be(
                "Translate the word 'Word' from the language 'da' into the languages 'en', 'ru'." + Environment.NewLine +
                "The word, in this context, means: 'Meaning'." + Environment.NewLine +
                "Provide between 1 and 3 possible answers so I can choose the best one." + Environment.NewLine +
                "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePromptForHeadword_WhenMeaningIsNotEmpty_AddsItToPrompt()
        {
            const string meaning = "Meaning1";

            string result = OpenAITranslationService.CreatePromptForHeadword(
                word: "Word",
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: meaning,
                partOfSpeech: "PartOfSpeech",
                examples: []);

            result.Should().Be(
                "Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'." + Environment.NewLine +
                "The word, in this context, means: 'Meaning1'." + Environment.NewLine +
                "Provide between 1 and 3 possible answers so I can choose the best one." + Environment.NewLine +
                "Maintain the same part of speech in the translations." + Environment.NewLine +
                "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePromptForHeadword_WhenMeaningIsEmpty_DoesNotAddItToPrompt()
        {
            const string meaning = "";

            string result = OpenAITranslationService.CreatePromptForHeadword(
                word: "Word",
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: meaning,
                partOfSpeech: "PartOfSpeech",
                examples: []);

            result.Should().Be(
                "Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'." + Environment.NewLine +
                "Provide between 1 and 3 possible answers so I can choose the best one." + Environment.NewLine +
                "Maintain the same part of speech in the translations." + Environment.NewLine +
                "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePromptForHeadword_WhenExamplesAreNotEmpty_AddsThemToPrompt()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];

            string result = OpenAITranslationService.CreatePromptForHeadword(
                word: "Word",
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: "Meaning",
                partOfSpeech: "PartOfSpeech",
                examples: examples);

            result.Should().Be(
                "Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'." + Environment.NewLine +
                "The word, in this context, means: 'Meaning'." + Environment.NewLine +
                "Check examples to better understand the context: 'example 1', 'example 2'." + Environment.NewLine +
                "Provide between 1 and 3 possible answers so I can choose the best one." + Environment.NewLine +
                "Maintain the same part of speech in the translations." + Environment.NewLine +
                "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePromptForHeadword_WhenExamplesAreEmpty_DoesNotAddThemToPrompt()
        {
            IEnumerable<string> examples = [];

            string result = OpenAITranslationService.CreatePromptForHeadword(
                word: "Word",
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: "Meaning",
                partOfSpeech: "PartOfSpeech",
                examples: examples);

            result.Should().Be(
                "Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'." + Environment.NewLine +
                "The word, in this context, means: 'Meaning'." + Environment.NewLine +
                "Provide between 1 and 3 possible answers so I can choose the best one." + Environment.NewLine +
                "Maintain the same part of speech in the translations." + Environment.NewLine +
                "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        #endregion

        #region Tests for CreatePromptForMeanings

        [TestMethod]
        public void CreatePromptForMeanings_WhenExamplesAreNotEmpty_AddsThemToPrompt()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];

            string result = OpenAITranslationService.CreatePromptForMeanings(
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meanings: [new Models.Input.Meaning(id: 1, Text: "Meaning", Examples: examples)]);

            result.Should().Be(
                "Translate strings from the language 'da' into the languages 'en', 'ru'." + Environment.NewLine +
                "Check examples to better understand the context." + Environment.NewLine +
                "In the output, retain the ID of the input text when returning translations." + Environment.NewLine +
                "id=\"1\", text=\"Meaning\", examples=\"'example 1', 'example 2'\".");
        }

        [TestMethod]
        public void CreatePromptForMeanings_WhenExamplesAreEmpty_DoesNotAddThemToPrompt()
        {
            IEnumerable<string> examples = [];

            string result = OpenAITranslationService.CreatePromptForMeanings(
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meanings: [new Models.Input.Meaning(id: 1, Text: "Meaning", Examples: examples)]);

            result.Should().Be(
                "Translate strings from the language 'da' into the languages 'en', 'ru'." + Environment.NewLine +
                "Check examples to better understand the context." + Environment.NewLine +
                "In the output, retain the ID of the input text when returning translations." + Environment.NewLine +
                "id=\"1\", text=\"Meaning\", examples=\"''\".");
        }

        #endregion

        #region Private methods

        private static ClientResult<ChatCompletion> CreateChatCompletionFromJson(string jsonFileName)
        {
            string jsonResponse = File.ReadAllText(Path.Combine(s_testDataPath, jsonFileName));

            ChatMessageContent content = [
                ChatMessageContentPart.CreateTextPart(jsonResponse)
            ];
            ChatCompletion chatCompletion = OpenAIChatModelFactory.ChatCompletion(content: content);

            ClientResult<ChatCompletion> clientResult = ClientResult.FromValue(chatCompletion, new Mock<PipelineResponse>().Object);
            return clientResult;
        }

        #endregion
    }
}
