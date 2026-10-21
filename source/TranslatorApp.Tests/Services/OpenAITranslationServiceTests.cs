// Ignore Spelling: App

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI.Chat;
using TranslatorApp.Models.Translation;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class OpenAITranslationServiceTests
    {
        #region Tests for TranslateAsync

        [TestMethod]
        public async Task TranslateAsync_Should_CallChatClient()
        {
            // Input doesn't matter, but it must have 2 definitions, because we mock 2 responses
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "es",
                DestinationLanguage: "de",
                Definition: new DefinitionInput(
                    Headword: new HeadwordInput(Text: "word to translate", PartOfSpeech: "noun", Meaning: "meaning", Examples: ["example 1"]),
                    Contexts: [
                        new ContextInput(
                            id: 1,
                            ContextString: "context 1",
                            Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", PartOfSpeech: "noun", Examples: ["meaning 1, example 1"]) ]
                        )
                    ]
            ));

            var openAIResponse = CreateChatCompletionFromJson("TranslationOutput_ES.json");

            var chatClientMock = new Mock<ChatClient>();
            chatClientMock
                .Setup(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(openAIResponse);

            var sut = new OpenAITranslationService(chatClientMock.Object, new Mock<ChatClient>(MockBehavior.Strict).Object, new Mock<ILaunchDarklyService>().Object, new Mock<ILogger<OpenAITranslationService>>().Object);

            TranslationOutput? result = await sut.TranslateAsync(translationInput, CancellationToken.None);

            result.Should().NotBeNull();

            DefinitionOutput definition;
            MeaningOutput meaning;

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            definition = result.Definition;

            // Check translations for headword
            definition.HeadwordTranslation.Should().Be("брить, сбривать, подстригать");
            definition.HeadwordTranslationEnglish.Should().Be("to shave, to trim, to barber");

            // Check translations for meanings
            definition.Contexts.Should().HaveCount(1);
            definition.Contexts[0].Meanings.Should().HaveCount(1);

            meaning = definition.Contexts[0].Meanings[0];
            meaning.id.Should().Be(1);
            meaning.MeaningTranslation.Should().Be("брить, сбривать, подстригать");
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_ReturnResultsFromResponse()
        {
            const string jsonResponse = """
                {
                  "results": [
                    " at spise ",
                    "et måltid",
                    "at spise",
                    "",
                    "en ret",
                    "et måltid",
                    "at indtage",
                    "at fortære",
                    "en portion",
                    "et indtag",
                    "en middag",
                    "at æde"
                  ]
                }
                """;

            var openAIResponse = CreateChatCompletionFromString(jsonResponse);

            var chatClientMock = new Mock<ChatClient>();
            chatClientMock
                .Setup(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(openAIResponse);

            var sut = new OpenAITranslationService(chatClientMock.Object, new Mock<ChatClient>(MockBehavior.Strict).Object, new Mock<ILaunchDarklyService>().Object, new Mock<ILogger<OpenAITranslationService>>().Object);

            IReadOnlyList<string> result = await sut.GetTranslationSuggestionsAsync("есть", "Russian", "Danish", CancellationToken.None);

            result.Should().Equal(
                " at spise ",
                "et måltid",
                "at spise",
                "",
                "en ret",
                "et måltid",
                "at indtage",
                "at fortære",
                "en portion",
                "et indtag",
                "en middag",
                "at æde");
        }

        #endregion

        #region Private methods

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, true)]
        [DataRow(true, false, true)]
        [DataRow(false, true, true)]
        public async Task ModelRollout_SelectsClientAndReasoning(bool useLuna, bool flagFails, bool translate)
        {
            var legacy = new Mock<ChatClient>(MockBehavior.Strict);
            var luna = new Mock<ChatClient>(MockBehavior.Strict);
            var flags = new Mock<ILaunchDarklyService>(MockBehavior.Strict);
            var flagSetup = flags.Setup(x => x.GetBooleanFlag(OpenAITranslationService.UseGpt6LunaFlag, false));
            if (flagFails)
                flagSetup.Throws(new TranslatorApp.Exceptions.LaunchDarklyGetFlagException("Evaluation failed"));
            else
                flagSetup.Returns(useLuna);

            var selected = useLuna ? luna : legacy;
#pragma warning disable OPENAI001 // Verify the experimental reasoning control used by the service.
            selected.Setup(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(),
                    It.Is<ChatCompletionOptions>(o => o.ResponseFormat != null &&
                        (useLuna ? o.ReasoningEffortLevel == new ChatReasoningEffortLevel("none") : o.ReasoningEffortLevel == null)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateChatCompletionFromString(translate ? "null" : "{\"results\":[\"at spise\"]}"));
#pragma warning restore OPENAI001

            var sut = new OpenAITranslationService(legacy.Object, luna.Object, flags.Object,
                new Mock<ILogger<OpenAITranslationService>>().Object);
            if (translate)
            {
                var input = new TranslationInput("2", "Danish", "Russian",
                    new DefinitionInput(new HeadwordInput("spise", "verb", "eat", []), []));
                await sut.TranslateAsync(input, CancellationToken.None);
            }
            else
            {
                var result = await sut.GetTranslationSuggestionsAsync("есть", "Russian", "Danish", CancellationToken.None);
                result.Should().Equal("at spise");
            }

            flags.Verify(x => x.GetBooleanFlag(OpenAITranslationService.UseGpt6LunaFlag, false), Times.Once);
            selected.Verify(x => x.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static ClientResult<ChatCompletion> CreateChatCompletionFromJson(string jsonFileName)
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string testDataFolderPath = Path.Combine(currentDir, "TestData");
            string jsonResponse = File.ReadAllText(Path.Combine(testDataFolderPath, jsonFileName));

            return CreateChatCompletionFromString(jsonResponse);
        }

        private static ClientResult<ChatCompletion> CreateChatCompletionFromString(string jsonResponse)
        {
            ChatMessageContent content = [
                ChatMessageContentPart.CreateTextPart(jsonResponse)
            ];

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ChatCompletion chatCompletion = OpenAIChatModelFactory.ChatCompletion(content: content);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            ClientResult<ChatCompletion> clientResult = ClientResult.FromValue(chatCompletion, new Mock<PipelineResponse>().Object);
            return clientResult;
        }

        #endregion
    }
}
