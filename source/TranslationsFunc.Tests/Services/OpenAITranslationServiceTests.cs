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

        #region Tests for Translate2Async

        [TestMethod]
        public async Task Translate2Async_Should_CallChatClient()
        {
            TranslationInput2 translationInput2 = _fixture.Create<TranslationInput2>();

            string headwordTranslationsResponse = File.ReadAllText(Path.Combine(s_testDataPath, "TranslationOutput.json"));

            ChatMessageContent headWordContent = [
                ChatMessageContentPart.CreateTextPart(headwordTranslationsResponse)
            ];
            ChatCompletion chatCompletion = OpenAIChatModelFactory.ChatCompletion(content: headWordContent);

            ClientResult<ChatCompletion> clientResult = ClientResult.FromValue(chatCompletion, new Mock<PipelineResponse>().Object);

            var chatClientMock = new Mock<ChatClient>();
            chatClientMock.Setup(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientResult);

            var sut = new OpenAITranslationService(chatClientMock.Object);

            TranslationOutput2 result = await sut.Translate2Async(translationInput2);

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
            // result.Meanings.Should().HaveCount(2);
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

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
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

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
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

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning1'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
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

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
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

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Check also examples to get a better context: 'example 1', 'example 2'. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
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

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        #endregion

        #region Tests for CreatePromptForMeaning

        [TestMethod]
        public void CreatePromptForMeaning_WhenWordIsEmptyButMeaningIsNot_CreatesPromptForMeaning()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];

            string result = OpenAITranslationService.CreatePromptForMeaning(
                sourceLanguage: "da",
                destinationLanguages: ["en", "ru"],
                meaning: "Meaning",
                examples: examples);

            result.Should().Be("Translate 'Meaning' from the language 'da' into the languages 'en', 'ru'. "
                + "Check also examples to get a better context: 'example 1', 'example 2'.");
        }

        #endregion
    }
}
