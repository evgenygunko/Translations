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
                DestinationLanguage: "ru",
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

            var sut = new OpenAITranslationService(chatClientMock.Object, new Mock<ILogger<OpenAITranslationService>>().Object);

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

        #endregion

        #region Private methods

        private static ClientResult<ChatCompletion> CreateChatCompletionFromJson(string jsonFileName)
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string testDataFolderPath = Path.Combine(currentDir, "TestData");
            string jsonResponse = File.ReadAllText(Path.Combine(testDataFolderPath, jsonFileName));

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
