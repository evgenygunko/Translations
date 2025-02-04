using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
using System.Text.Json;
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

            string mockResponse = File.ReadAllText(Path.Combine(s_testDataPath, "TranslationOutput2.json"));
            var translationOutput2 = JsonSerializer.Deserialize<TranslationOutput2>(mockResponse);
            translationOutput2.Should().NotBeNull();
            translationOutput2!.Headword.Should().NotBeNull();
            translationOutput2!.Meanings.Should().HaveCount(2);

            ChatMessageContent content = [
                ChatMessageContentPart.CreateTextPart(mockResponse)
            ];
            ChatCompletion chatCompletion = OpenAIChatModelFactory.ChatCompletion(content: content);

            ClientResult<ChatCompletion> clientResult = ClientResult.FromValue(chatCompletion, new Mock<PipelineResponse>().Object);

            var chatClientMock = new Mock<ChatClient>();
            chatClientMock.Setup(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(clientResult);

            var sut = new OpenAITranslationService(chatClientMock.Object);

            TranslationOutput2 result = await sut.Translate2Async(translationInput2);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(translationOutput2);
        }

        #endregion

        #region Tests for CreatePrompt

        #region Translation for Word

        [TestMethod]
        public void CreatePrompt_WhenPartOfSpeechIsNotEmpty_AddsItToPrompt()
        {
            const string partOfSpeech = "PartOfSpeech";
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "Word", Meaning: "Meaning", PartOfSpeech: partOfSpeech, Examples: []);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenPartOfSpeechIsEmpty_DoesNotAddItToPrompt()
        {
            const string partOfSpeech = "";
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "Word", Meaning: "Meaning", PartOfSpeech: partOfSpeech, Examples: []);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenMeaningIsNotEmpty_AddsItToPrompt()
        {
            const string meaning = "Meaning1";
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "Word", Meaning: meaning, PartOfSpeech: "PartOfSpeech", Examples: []);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning1'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenMeaningIsEmpty_DoesNotAddItToPrompt()
        {
            const string meaning = "";
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "Word", Meaning: meaning, PartOfSpeech: "PartOfSpeech", Examples: []);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenExamplesAreNotEmpty_AddsThemToPrompt()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "Word", Meaning: "Meaning", PartOfSpeech: "PartOfSpeech", Examples: examples);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Check also examples to get a better context: 'example 1', 'example 2'. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenExamplesAreEmpty_DoesNotAddThemToPrompt()
        {
            IEnumerable<string> examples = [];
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "Word", Meaning: "Meaning", PartOfSpeech: "PartOfSpeech", Examples: examples);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        #endregion

        #region Translations for Meaning

        [TestMethod]
        public void CreatePrompt_WhenWordIsEmptyButMeaningIsNot_CreatesPromptForMeaning()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];
            TranslationInput input = new TranslationInput(SourceLanguage: "da", DestinationLanguages: ["en", "ru"], Word: "", Meaning: "Meaning", PartOfSpeech: "", Examples: examples);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate 'Meaning' from the language 'da' into the languages 'en', 'ru'. "
                + "Check also examples to get a better context: 'example 1', 'example 2'.");
        }

        #endregion

        #endregion
    }
}
