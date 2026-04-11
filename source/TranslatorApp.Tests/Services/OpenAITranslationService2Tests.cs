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

            var loggerMock = new Mock<ILogger<OpenAITranslationService2>>();

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                loggerMock.Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

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

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    new EventId((int)TranslatorAppEventId.TranslationReceived),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("The call to OpenAPI Response API took")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
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
                Definition: new DefinitionInput(
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
                ));

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
                Definition: new DefinitionInput(
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
                ));

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
            deserializedInput!.Definition.Headword.Text.Should().Be("quote\"test");
            deserializedInput.Definition.Headword.Examples.First().Should().Be("He said \"hello\"");
            deserializedInput.Definition.Contexts.First().ContextString.Should().Be("Context with \"quotes\"");
        }

        [TestMethod]
        public void GetPromptMessage_Should_HandleSpecialCharactersInLanguages()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "zh-CN",
                DestinationLanguage: "ja-JP",
                Definition: new DefinitionInput(
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
                ));

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
        public void GetPromptMessage_Should_HandleNullValues()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "es",
                Definition: new DefinitionInput(
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
                ));

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
            deserializedInput.Definition.Headword.PartOfSpeech.Should().BeNull();
            deserializedInput.Definition.Contexts.First().ContextString.Should().BeNull();
        }

        [TestMethod]
        public void GetPromptMessage_Should_PreservePromptId()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "en",
                DestinationLanguage: "fr",
                Definition: new DefinitionInput(
                    Headword: new HeadwordInput(Text: string.Empty, PartOfSpeech: null, Meaning: "meaning", Examples: Array.Empty<string>()),
                    Contexts: Array.Empty<ContextInput>())
                );

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
                Definition: new DefinitionInput(
                    Headword: new HeadwordInput(Text: string.Empty, PartOfSpeech: null, Meaning: "meaning", Examples: Array.Empty<string>()),
                    Contexts: Array.Empty<ContextInput>())
            );

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
                Definition: new DefinitionInput(
                    Headword: new HeadwordInput(Text: string.Empty, PartOfSpeech: null, Meaning: "meaning", Examples: Array.Empty<string>()),
                    Contexts: Array.Empty<ContextInput>())
            );

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

        #region Tests for GetTranslationSuggestionsPromptMessage

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_ReturnValidJsonStructure()
        {
            // Arrange
            var inputText = "Hello, how are you?";
            var sourceLanguage = "en";
            var destinationLanguage = "fr";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Validate that the result is valid JSON
            using var document = JsonDocument.Parse(result);
            var root = document.RootElement;

            root.TryGetProperty("prompt", out var promptElement).Should().BeTrue();
            promptElement.TryGetProperty("id", out var idElement).Should().BeTrue();
            promptElement.TryGetProperty("variables", out var variablesElement).Should().BeTrue();

            idElement.GetString().Should().Be("suggestions_prompt_123");

            variablesElement.TryGetProperty("input_text", out var inputTextElement).Should().BeTrue();
            variablesElement.TryGetProperty("source_language", out var sourceLangElement).Should().BeTrue();
            variablesElement.TryGetProperty("destination_language", out var destLangElement).Should().BeTrue();

            inputTextElement.GetString().Should().Be(inputText);
            sourceLangElement.GetString().Should().Be(sourceLanguage);
            destLangElement.GetString().Should().Be(destinationLanguage);
        }

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_ProperlyEscapeSpecialCharactersInInputText()
        {
            // Arrange
            var inputText = "What is \"hello\" and 'world'? \\ test";
            var sourceLanguage = "en";
            var destinationLanguage = "es";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            result.Should().NotBeNullOrEmpty();

            // Should be valid JSON despite special characters in the input
            Action parseAction = () => JsonDocument.Parse(result);
            parseAction.Should().NotThrow("the result should be valid JSON");

            using var document = JsonDocument.Parse(result);
            var retrievedInputText = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_text")
                .GetString();

            retrievedInputText.Should().Be(inputText);
        }

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_HandleEmptyInputText()
        {
            // Arrange
            var inputText = string.Empty;
            var sourceLanguage = "en";
            var destinationLanguage = "de";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            result.Should().NotBeNullOrEmpty();

            using var document = JsonDocument.Parse(result);
            var retrievedInputText = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_text")
                .GetString();

            retrievedInputText.Should().Be(string.Empty);
        }

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_HandleMultilineInputText()
        {
            // Arrange
            var inputText = "Line 1\nLine 2\nLine 3";
            var sourceLanguage = "en";
            var destinationLanguage = "ja";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            result.Should().NotBeNullOrEmpty();

            using var document = JsonDocument.Parse(result);
            var retrievedInputText = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_text")
                .GetString();

            retrievedInputText.Should().Be(inputText);
        }

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_PreserveSuggestionsPromptId()
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "fr";
            var promptId = "custom_suggestions_prompt_789";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = promptId
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            using var document = JsonDocument.Parse(result);
            var retrievedPromptId = document.RootElement
                .GetProperty("prompt")
                .GetProperty("id")
                .GetString();

            retrievedPromptId.Should().Be(promptId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("current")]
        public void GetTranslationSuggestionsPromptMessage_WhenLDFlagIsEmptyOrCurrent_DoesNotAddPromptVersion(string ldFlagValue)
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "fr";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var launchDarklyServiceMock = new Mock<ILaunchDarklyService>();
            launchDarklyServiceMock
                .Setup(x => x.GetStringFlag("open-ai-prompt-version", ""))
                .Returns(ldFlagValue);

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                launchDarklyServiceMock.Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            using var document = JsonDocument.Parse(result);
            var promptElement = document.RootElement.GetProperty("prompt");

            bool hasVersionProperty = promptElement.TryGetProperty("version", out _);
            hasVersionProperty.Should().BeFalse();
        }

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_AddPromptVersionProperty()
        {
            // Arrange
            const string ldFlagValue = "v2.5";
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "es";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var launchDarklyServiceMock = new Mock<ILaunchDarklyService>();
            launchDarklyServiceMock
                .Setup(x => x.GetStringFlag("open-ai-prompt-version", ""))
                .Returns(ldFlagValue);

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                launchDarklyServiceMock.Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            using var document = JsonDocument.Parse(result);
            var promptVersion = document.RootElement
                .GetProperty("prompt")
                .GetProperty("version")
                .GetString();

            promptVersion.Should().Be(ldFlagValue);
        }

        [TestMethod]
        public void GetTranslationSuggestionsPromptMessage_Should_HandleUnicodeCharacters()
        {
            // Arrange
            var inputText = "你好世界 🌍 مرحبا";
            var sourceLanguage = "zh";
            var destinationLanguage = "ar";

            var openAIResponseClientMock = new Mock<ResponsesClient>();

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            string result = sut.GetTranslationSuggestionsPromptMessage(inputText, sourceLanguage, destinationLanguage);

            // Assert
            result.Should().NotBeNullOrEmpty();

            using var document = JsonDocument.Parse(result);
            var retrievedInputText = document.RootElement
                .GetProperty("prompt")
                .GetProperty("variables")
                .GetProperty("input_text")
                .GetString();

            retrievedInputText.Should().Be(inputText);
        }

        #endregion

        #region Tests for GetTranslationSuggestionsAsync

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_ReturnTranslationSuggestions()
        {
            // Arrange
            var inputText = "hello";
            var sourceLanguage = "en";
            var destinationLanguage = "es";
            var suggestions = new[] { "hola", "buenos días", "saludos" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().ContainInOrder("hola", "buenos días", "saludos");
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleEmptyResults()
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "de";

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: Array.Empty<string>());
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleSingleSuggestion()
        {
            // Arrange
            var inputText = "bonjour";
            var sourceLanguage = "fr";
            var destinationLanguage = "en";
            var suggestions = new[] { "hello" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Should().Be("hello");
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleMultipleSuggestions()
        {
            // Arrange
            var inputText = "gato";
            var sourceLanguage = "es";
            var destinationLanguage = "en";
            var suggestions = new[] { "cat", "feline", "tomcat", "mouser", "pussy" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.Should().ContainInOrder("cat", "feline", "tomcat", "mouser", "pussy");
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleNullResponse()
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "fr";

            // Mock a response that returns null
            var openAIResponse = CreateResponseFromTranslationSuggestions(null);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var loggerMock = new Mock<ILogger<OpenAITranslationService2>>();

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                loggerMock.Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No text found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleSuggestionsWithSpecialCharacters()
        {
            // Arrange
            var inputText = "café";
            var sourceLanguage = "fr";
            var destinationLanguage = "en";
            var suggestions = new[] { "coffee", "café", "caf'e" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain("café");
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleUnicodeCharacters()
        {
            // Arrange
            var inputText = "你好";
            var sourceLanguage = "zh";
            var destinationLanguage = "en";
            var suggestions = new[] { "hello", "hi", "good day" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().ContainInOrder("hello", "hi", "good day");
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_PassCorrectParametersToPromptMessage()
        {
            // Arrange
            var inputText = "test input";
            var sourceLanguage = "en";
            var destinationLanguage = "de";
            var suggestions = new[] { "test" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            BinaryContent? capturedContent = null;
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .Callback<BinaryContent, RequestOptions>((content, options) => capturedContent = content)
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            openAIResponseClientMock.Verify(
                x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()),
                Times.Once);
            capturedContent.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_ReturnReadOnlyList()
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "es";
            var suggestions = new[] { "prueba", "examen" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            var result = await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<IReadOnlyList<string>>();
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_LogInformationAboutExecutionTime()
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "fr";
            var suggestions = new[] { "test" };

            var translationSuggestionsOutput = new TranslationSuggestionsOutput(results: suggestions);
            var openAIResponse = CreateResponseFromTranslationSuggestions(translationSuggestionsOutput);

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(openAIResponse);

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var loggerMock = new Mock<ILogger<OpenAITranslationService2>>();

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                loggerMock.Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act
            await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, CancellationToken.None);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    new EventId((int)TranslatorAppEventId.TranslationSuggestionsReceived),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("translation suggestions")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task GetTranslationSuggestionsAsync_Should_HandleCancellationToken()
        {
            // Arrange
            var inputText = "test";
            var sourceLanguage = "en";
            var destinationLanguage = "es";
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var openAIResponseClientMock = new Mock<ResponsesClient>();
            openAIResponseClientMock
                .Setup(x => x.CreateResponseAsync(It.IsAny<BinaryContent>(), It.IsAny<RequestOptions>()))
                .ThrowsAsync(new OperationCanceledException());

            var openAIConfigurationMock = new Mock<IOptions<OpenAIConfiguration>>();
            openAIConfigurationMock.Setup(x => x.Value).Returns(new OpenAIConfiguration
            {
                SuggestionsPromptId = "suggestions_prompt_123"
            });

            var sut = new OpenAITranslationService2(
                openAIResponseClientMock.Object,
                new Mock<ILogger<OpenAITranslationService2>>().Object,
                openAIConfigurationMock.Object,
                new Mock<ILaunchDarklyService>().Object);

            // Act & Assert
            Func<Task> act = async () => await sut.GetTranslationSuggestionsAsync(inputText, sourceLanguage, destinationLanguage, cancellationTokenSource.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
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

        private static ClientResult CreateResponseFromTranslationSuggestions(TranslationSuggestionsOutput? output)
        {
            string? suggestionJson = output == null ? null : JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = false });

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
                                text = suggestionJson ?? ""
                            }
                        },
                        role = "assistant"
                    }
                }
            };

            string responseDataJson = JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = false });

            Mock<PipelineResponse> pipelineResponse = new Mock<PipelineResponse>();
            pipelineResponse.SetupGet(x => x.Content).Returns(BinaryData.FromString(responseDataJson));

            ClientResult<string> clientResult = ClientResult.FromValue("A response from unit test", pipelineResponse.Object);
            return clientResult;
        }

        #endregion
    }
}