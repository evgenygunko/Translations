// Ignore Spelling: Deserialize

using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using My.Function;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Models.Output;
using TranslationsFunc.Services;

namespace TranslationFunc.Tests
{
    [TestClass]
    public class HttpTranslateTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        #region Tests for Run

        [TestMethod]
        public async Task Run_WhenInputDataIsNull_ReturnsBadRequest()
        {
            TranslationInput? translationInput = null;
            var httpRequestData = MockHttpRequestData.Create(translationInput, []);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();
            responseBody.Should().StartWith("{\"Error\":\"Input data is null");
        }

        [TestMethod]
        public async Task Run_WhenCannotDeserializeInput_ReturnsBadRequest()
        {
            string input = "{ \"prop1\" : \"abc\" }";
            var httpRequestData = MockHttpRequestData.Create(input, []);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();
            responseBody.Should().StartWith("{\"Error\":\"Cannot deserialize input data. Exception:");
        }

        #region Tests for version 1

        [TestMethod]
        public async Task Run_WhenValidationDoesNotPass_ReturnsBadRequest()
        {
            var translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "",
                DestinationLanguages: ["ru", "en"],
                Headword: new TranslationsFunc.Models.Input.Headword(Text: "word to translate", Meaning: "meaning", PartOfSpeech: "noun", Examples: ["example 1"]),
                Meanings: [
                    new TranslationsFunc.Models.Input.Meaning(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                    new TranslationsFunc.Models.Input.Meaning(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                    ]);

            var httpRequestData = MockHttpRequestData.Create(translationInput, []);

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SourceLanguage", "SourceLanguage cannot be null or empty"));

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslationInput>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();
            responseBody.Should().Be("{\"Error\":\"Error: SourceLanguage cannot be null or empty.\"}");
        }

        [TestMethod]
        public async Task Run_WhenExceptionOccurs_LogsError()
        {
            var translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: new TranslationsFunc.Models.Input.Headword(Text: "word to translate", Meaning: "meaning", PartOfSpeech: "noun", Examples: ["example 1"]),
                Meanings: [
                    new TranslationsFunc.Models.Input.Meaning(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                    new TranslationsFunc.Models.Input.Meaning(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                    ]);
            var httpRequestData = MockHttpRequestData.Create(translationInput, []);
            var translationOutput = _fixture.Create<TranslationOutput>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ThrowsAsync(new Exception("exception from unit test"));

            var loggerMock = _fixture.Create<Mock<ILogger<HttpTranslate>>>();

            var loggerFactoryMock = _fixture.Freeze<Mock<ILoggerFactory>>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var sut = _fixture.Create<HttpTranslate>();

            await sut.Invoking(x => x.Run(httpRequestData))
                .Should().ThrowAsync<Exception>()
                .WithMessage("exception from unit test");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while calling translator API.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task Run_Should_CallsOpenAITranslationServiceAndReturnTranslationOutput()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "1",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Headword: new TranslationsFunc.Models.Input.Headword(Text: "word to translate", Meaning: "meaning", PartOfSpeech: "noun", Examples: ["example 1"]),
                Meanings: [
                    new TranslationsFunc.Models.Input.Meaning(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                    new TranslationsFunc.Models.Input.Meaning(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                    ]);

            NameValueCollection query = new();
            query["service"] = "azure";

            var httpRequestData = MockHttpRequestData.Create(translationInput, query);
            var translationOutput2 = _fixture.Create<TranslationOutput>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ReturnsAsync(translationOutput2);

            // Act
            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();

            var deserializedResponse = JsonSerializer.Deserialize<TranslationOutput>(responseBody);
            deserializedResponse.Should().BeEquivalentTo(translationOutput2);

            openAITranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslationInput>()));
        }

        #endregion

        #region Tests for version 2

        [TestMethod]
        public async Task Run_WhenTranslationInput2ModelIsNotValid_ReturnsBadRequest()
        {
            var translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "",
                DestinationLanguages: ["ru", "en"],
                Definitions: [
                    new Definition(
                        id: 1,
                        Headword: new TranslationsFunc.Models.Input.Headword(Text: "word to translate", Meaning: "meaning", PartOfSpeech: "noun", Examples: ["example 1"]),
                        Contexts: [
                            new TranslationsFunc.Models.Input.Context(
                                id: 1,
                                ContextEN: "context 1",
                                Meanings: [
                                    new TranslationsFunc.Models.Input.Meaning(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                                    new TranslationsFunc.Models.Input.Meaning(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                                ]
                            ),
                        ]
                    )
                ]);

            var httpRequestData = MockHttpRequestData.Create(translationInput, []);

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SourceLanguage", "SourceLanguage cannot be null or empty"));

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslationInput2>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslationInput2>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();
            responseBody.Should().Be("{\"Error\":\"Error: SourceLanguage cannot be null or empty.\"}");
        }

        [TestMethod]
        public async Task Run_WhenTranslate2AsyncThrowsException_LogsError()
        {
            var translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Definitions: [
                    new Definition(
                        id: 1,
                        Headword: new TranslationsFunc.Models.Input.Headword(Text: "word to translate", Meaning: "meaning", PartOfSpeech: "noun", Examples: ["example 1"]),
                        Contexts: [
                            new TranslationsFunc.Models.Input.Context(
                                id: 1,
                                ContextEN: "context 1",
                                Meanings: [
                                    new TranslationsFunc.Models.Input.Meaning(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                                    new TranslationsFunc.Models.Input.Meaning(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                                ]
                            )
                        ]
                    )
                ]);

            var httpRequestData = MockHttpRequestData.Create(translationInput, []);
            var translationOutput = _fixture.Create<TranslationOutput2>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.Translate2Async(It.IsAny<TranslationInput2>())).ThrowsAsync(new Exception("exception from unit test"));

            var loggerMock = _fixture.Create<Mock<ILogger<HttpTranslate>>>();

            var loggerFactoryMock = _fixture.Freeze<Mock<ILoggerFactory>>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var sut = _fixture.Create<HttpTranslate>();

            await sut.Invoking(x => x.Run(httpRequestData))
                .Should().ThrowAsync<Exception>()
                .WithMessage("exception from unit test");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while calling translator API.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task Run_Should_CallsOpenAITranslationServiceAndReturnTranslationOutput2()
        {
            // Arrange
            var translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Definitions: [
                    new Definition(
                        id: 1,
                        Headword: new TranslationsFunc.Models.Input.Headword(Text: "word to translate", Meaning: "meaning", PartOfSpeech: "noun", Examples: ["example 1"]),
                        Contexts: [
                            new TranslationsFunc.Models.Input.Context(
                                id: 1,
                                ContextEN: "context 1",
                                Meanings: [
                                    new TranslationsFunc.Models.Input.Meaning(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                                    new TranslationsFunc.Models.Input.Meaning(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                                ]
                            )
                        ]
                    )
                ]);

            var httpRequestData = MockHttpRequestData.Create(translationInput, new NameValueCollection());
            var translationOutput2 = _fixture.Create<TranslationOutput2>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.Translate2Async(It.IsAny<TranslationInput2>())).ReturnsAsync(translationOutput2);

            // Act
            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();

            var deserializedResponse = JsonSerializer.Deserialize<TranslationOutput2>(responseBody);
            deserializedResponse.Should().BeEquivalentTo(translationOutput2);

            openAITranslationServiceMock.Verify(x => x.Translate2Async(It.IsAny<TranslationInput2>()));
        }

        #endregion

        #endregion

        #region Tests for FormatValidationErrorMessage

        [TestMethod]
        public void FormatValidationErrorMessage_WhenOneError_AddsFullStopAtTheEnd()
        {
            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("property1", "property1 cannot be null"));

            var sut = _fixture.Create<HttpTranslate>();
            string result = sut.FormatValidationErrorMessage(validationResult);

            result.Should().Be("Error: property1 cannot be null.");
        }

        [TestMethod]
        public void FormatValidationErrorMessage_WhenTwoErrors_AddsFullStopAtTheEnd()
        {
            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("property1", "property1 cannot be null"));
            validationResult.Errors.Add(new ValidationFailure("property2", "property2 cannot be null"));

            var sut = _fixture.Create<HttpTranslate>();
            string result = sut.FormatValidationErrorMessage(validationResult);

            result.Should().Be("Error: property1 cannot be null, property2 cannot be null.");
        }

        #endregion
    }
}
