using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Responses;
using TranslatorApp.Models;
using TranslatorApp.Models.Translation;
using TranslatorApp.Services;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class OpenAITranslationService2Tests
    {
        #region Tests for TranslateAsync

        [TestMethod]
        public async Task TranslateAsync_Should_ReturnTranslationOutput()
        {
            // Input doesn't matter, but it must have 2 definitions, because we mock 2 responses
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "es",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        Headword: new HeadwordInput(Text: "word to translate", PartOfSpeech: "noun", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts:[
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", PartOfSpeech: "noun", Examples: ["meaning 1, example 1"]) ]
                            )
                        ]
                    ),
                    new DefinitionInput(
                        id: 2,
                        Headword: new HeadwordInput(Text: "word to translate", PartOfSpeech : "noun", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", PartOfSpeech: "noun", Examples: ["meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var openAIResponse = CreateReponseFromJson("TranslationOutput_ES.json");

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            TranslationOutput result = await sut.TranslateAsync(translationInput, CancellationToken.None);

            result.Should().NotBeNull();

            result.Definitions.Should().HaveCount(2);

            DefinitionOutput definition;
            MeaningOutput meaning;

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            definition = result.Definitions[0];
            definition.id.Should().Be(1);

            // Check translations for headword
            definition.HeadwordTranslation.Should().Be("брить, сбривать, подстригать");
            definition.HeadwordTranslationEnglish.Should().Be("to shave, to trim, to barber");

            // Check translations for meanings
            definition.Contexts.Should().HaveCount(1);
            definition.Contexts[0].Meanings.Should().HaveCount(1);

            meaning = definition.Contexts[0].Meanings[0];
            meaning.id.Should().Be(1);
            meaning.MeaningTranslation.Should().Be("брить, сбривать, подстригать");

            /***********************************************************************/
            // Afeitarse
            /***********************************************************************/
            definition = result.Definitions[1];
            definition.id.Should().Be(2);

            // Check translations for headword
            definition.HeadwordTranslation.Should().Be("бриться, побриться, подстричься");
            definition.HeadwordTranslationEnglish.Should().Be("to shave oneself, to be shaved, to trim oneself");

            // Check translations for meanings
            definition.Contexts.Should().HaveCount(1);
            definition.Contexts[0].Meanings.Should().HaveCount(1);

            meaning = definition.Contexts[0].Meanings[0];
            meaning.id.Should().Be(1);
            meaning.MeaningTranslation.Should().Be("бриться, побриться, подстричься");
        }

        #endregion

        #region Tests for GetPromptMessage

        [TestMethod]
        public void GetPromptMessage_Should_ReturnValidJsonStructure()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "fr",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        Headword: new HeadwordInput(Text: "hello", PartOfSpeech: "interjection", Meaning: "greeting", Examples: ["Hello, world!"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "Casual greeting",
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "friendly greeting", PartOfSpeech: "interjection", Examples: ["Hello there!"])
                                ]
                            )
                        ]
                    )
                ]);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Validate that the result is valid JSON
            JsonDocument.Parse(result);

            // Parse and validate structure
            using var document = JsonDocument.Parse(result);
            var root = document.RootElement;

            root.TryGetProperty("prompt", out var promptElement).Should().BeTrue();
            promptElement.TryGetProperty("id", out var idElement).Should().BeTrue();
            promptElement.TryGetProperty("variables", out var variablesElement).Should().BeTrue();

            idElement.GetString().Should().Be("prompt_123");

            variablesElement.TryGetProperty("source_language", out var sourceLangElement).Should().BeTrue();
            variablesElement.TryGetProperty("destination_language", out var destLangElement).Should().BeTrue();
            variablesElement.TryGetProperty("input_json", out var inputJsonElement).Should().BeTrue();

            sourceLangElement.GetString().Should().Be("en");
            destLangElement.GetString().Should().Be("fr");
            inputJsonElement.GetString().Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void GetPromptMessage_Should_ProperlyEscapeJsonInInputJson()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "fr",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        Headword: new HeadwordInput(Text: "quote\"test", PartOfSpeech: "noun", Meaning: "test with quotes", Examples: ["He said \"hello\""]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "Context with \"quotes\"",
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "meaning with \"quotes\"", PartOfSpeech: "noun", Examples: ["Example with \"quotes\""])
                                ]
                            )
                        ]
                    )
                ]);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Trim the result to remove leading/trailing whitespace that might cause JSON parsing issues
            string trimmedResult = result.Trim();

            // Should be valid JSON despite quotes in the input
            Action parseAction = () => JsonDocument.Parse(trimmedResult);
            parseAction.Should().NotThrow("the result should be valid JSON");

            using var document = JsonDocument.Parse(trimmedResult);
            var inputJsonString = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_json")
                .GetString();

            inputJsonString.Should().NotBeNullOrEmpty();

            // The input_json should be properly escaped - verify it's valid JSON that can be parsed
            Action parseInputJsonAction = () => JsonDocument.Parse(inputJsonString!);
            parseInputJsonAction.Should().NotThrow("the input_json should be valid JSON");

            // Should be able to deserialize the escaped JSON back to the original object
            var deserializedInput = JsonSerializer.Deserialize<TranslationInput>(inputJsonString!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            deserializedInput.Should().NotBeNull();
            deserializedInput!.Definitions.First().Headword.Text.Should().Be("quote\"test");
            deserializedInput.Definitions.First().Headword.Examples.First().Should().Be("He said \"hello\"");
            deserializedInput.Definitions.First().Contexts.First().ContextString.Should().Be("Context with \"quotes\"");
        }

        [TestMethod]
        public void GetPromptMessage_Should_HandleSpecialCharactersInLanguages()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "zh-CN",
                DestinationLanguage: "ja-JP",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        Headword: new HeadwordInput(Text: "你好", PartOfSpeech: "interjection", Meaning: "greeting", Examples: ["你好世界"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "友好问候",
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "问候语", PartOfSpeech: "interjection", Examples: ["你好吗？"])
                                ]
                            )
                        ]
                    )
                ]);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Should be valid JSON
            JsonDocument.Parse(result);

            using var document = JsonDocument.Parse(result);
            var variables = document.RootElement.GetProperty("prompt").GetProperty("variables");

            variables.GetProperty("source_language").GetString().Should().Be("zh-CN");
            variables.GetProperty("destination_language").GetString().Should().Be("ja-JP");
        }

        [TestMethod]
        public void GetPromptMessage_Should_HandleEmptyDefinitions()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "es",
                Definitions: []);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Should be valid JSON
            JsonDocument.Parse(result);

            using var document = JsonDocument.Parse(result);
            var inputJsonString = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_json")
                .GetString();

            var deserializedInput = JsonSerializer.Deserialize<TranslationInput>(inputJsonString!);
            deserializedInput.Should().NotBeNull();
            deserializedInput.Definitions.Should().BeEmpty();
        }

        [TestMethod]
        public void GetPromptMessage_Should_HandleNullValues()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "es",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        Headword: new HeadwordInput(Text: "test", PartOfSpeech: null, Meaning: "test meaning", Examples: ["test example"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: null,
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "meaning", PartOfSpeech: null, Examples: ["example"])
                                ]
                            )
                        ]
                    )
                ]);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Should be valid JSON
            JsonDocument.Parse(result);

            using var document = JsonDocument.Parse(result);
            var inputJsonString = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_json")
                .GetString();

            var deserializedInput = JsonSerializer.Deserialize<TranslationInput>(inputJsonString!);
            deserializedInput.Should().NotBeNull();
            deserializedInput.Definitions.First().Headword.PartOfSpeech.Should().BeNull();
            deserializedInput.Definitions.First().Contexts.First().ContextString.Should().BeNull();
        }

        [TestMethod]
        public void GetPromptMessage_Should_PreservePromptId()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "fr",
                Definitions: []);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            using var document = JsonDocument.Parse(result);
            var promptId = document.RootElement
                .GetProperty("prompt")
                .GetProperty("id")
                .GetString();

            promptId.Should().Be("prompt_123");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("current")]
        public void GetPromptMessage_WhenLDFLagIsEmptyOrCurrent_DoesNotAddPromptVersion(string ldFLagValue)
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "fr",
                Definitions: []);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var launchDarklyServiceMock = new Mock<ILaunchDarklyService>();
            launchDarklyServiceMock
                .Setup(x => x.GetStringFlag("open-ai-prompt-version", ""))
                .Returns(ldFLagValue);

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                launchDarklyServiceMock.Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            using var document = JsonDocument.Parse(result);
            var promptElement = document.RootElement.GetProperty("prompt");

            bool hasVersionProperty = promptElement.TryGetProperty("version", out _);
            hasVersionProperty.Should().BeFalse();
        }

        [TestMethod]
        public void GetPromptMessage_Should_AddPromptVersionProperty()
        {
            const string ldFLagValue = "v5.0";

            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "fr",
                Definitions: []);

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                PromptId = "prompt_123"
            });

            var launchDarklyServiceMock = new Mock<ILaunchDarklyService>();
            launchDarklyServiceMock
                .Setup(x => x.GetStringFlag("open-ai-prompt-version", ""))
                .Returns(ldFLagValue);

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                launchDarklyServiceMock.Object);

            // Act
            string result = sut.GetPromptMessage(translationInput);

            // Assert
            using var document = JsonDocument.Parse(result);
            var promptVersion = document.RootElement
                .GetProperty("prompt")
                .GetProperty("version")
                .GetString();

            promptVersion.Should().Be(ldFLagValue);
        }

        #endregion

        #region Private methods

        private static ClientResult CreateReponseFromJson(string jsonFileName)
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string testDataFolderPath = Path.Combine(currentDir, "TestData");
            string responseJson = File.ReadAllText(Path.Combine(testDataFolderPath, jsonFileName));

            // Mock response object from OpenAI Response API
            var responseData = new
            {
                id = "resp_123",
                model = "gpt-4.1-mini-2025-04-14",
                output = new[]
                {
                    new
                    {
                        id = "msg_689f781876a481a298eb40963aedb4ad0b76e9c7b0aa83ca",
                        type = "message",
                        status = "completed",
                        content = new[]
                        {
                            new
                            {
                                type = "output_text",
                                text = responseJson
                            }
                        },
                        role = "assistant"
                    }
                }
            };

            string responseDataJson = JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });

            Mock<PipelineResponse> pipelineResponse = new Mock<PipelineResponse>();
            pipelineResponse.SetupGet(x => x.Content).Returns(BinaryData.FromString(responseDataJson));

            ClientResult<string> clientResult = ClientResult.FromValue("A response from unit test", pipelineResponse.Object);
            return clientResult;
        }

        #endregion
    }
}