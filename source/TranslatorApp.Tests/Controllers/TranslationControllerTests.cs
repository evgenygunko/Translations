// Ignore Spelling: Deserialize

using System.Net;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Controllers;
using TranslatorApp.Models;
using TranslatorApp.Models.Input;
using TranslatorApp.Models.Output;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Controllers
{
    [TestClass]
    public class TranslationControllerTests
    {
        private readonly IFixture _fixture = FixtureFactory.CreateWithControllerCustomizations();

        #region Tests for TranslateAsync

        [TestMethod]
        public async Task TranslateAsync_WhenInputDataIsNull_ReturnsBadRequest()
        {
            TranslationInput? translationInput = null;

            var sut = _fixture.Create<TranslationController>();
            ActionResult<TranslationOutput> actionResult = await sut.TranslateAsync(translationInput!);

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Input data is null");
        }

        [DataTestMethod]
        [DataRow("1")]
        [DataRow("3")]
        public async Task TranslateAsync_WhenUnsupportedProtocolVersion_ReturnsBadRequest(string protocolVersion)
        {
            TranslationInput? translationInput = new TranslationInput(
                Version: protocolVersion,
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Definitions: []);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<TranslationOutput> actionResult = await sut.TranslateAsync(translationInput!);

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Only protocol version 2 is supported.");
        }

        [TestMethod]
        public async Task TranslateAsync_WhenModelIsNotValid_ReturnsBadRequest()
        {
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "noun",
                        Headword: new HeadwordInput(Text: "word to translate", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                                    new MeaningInput(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                                ]
                            ),
                        ]
                    )
                ]);

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SourceLanguage", "SourceLanguage cannot be null or empty"));

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslationInput>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<TranslationOutput> actionResult = await sut.TranslateAsync(translationInput);

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Error: SourceLanguage cannot be null or empty.");
        }

        [TestMethod]
        public async Task TranslateAsync_WhenOpenAITranslationServiceThrowsException_LogsError()
        {
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "noun",
                        Headword: new HeadwordInput(Text: "word to translate", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                                    new MeaningInput(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                                ]
                            )
                        ]
                    )
                ]);

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslationInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ThrowsAsync(new Exception("exception from unit test"));

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var sut = _fixture.Create<TranslationController>();

            await sut.Invoking(x => x.TranslateAsync(translationInput))
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
        public async Task TranslateAsync_Should_CallsOpenAITranslationServiceAndReturnTranslationOutput()
        {
            // Arrange
            var translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "noun",
                        Headword: new HeadwordInput(Text: "word to translate", Meaning: "meaning", Examples: ["example 1"]),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [
                                    new MeaningInput(id: 1, Text: "meaning 1", Examples: [ "meaning 1, example 1" ]),
                                    new MeaningInput(id: 2, Text: "meaning 2", Examples: [ "meaning 2, example 1", "meaning 2, example 2" ]),
                                ]
                            )
                        ]
                    )
                ]);

            var translationOutput = _fixture.Create<TranslationOutput>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ReturnsAsync(translationOutput);

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslationInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<TranslationOutput> actionResult = await sut.TranslateAsync(translationInput);

            // Assert
            actionResult.Value.Should().BeEquivalentTo(translationOutput);

            openAITranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslationInput>()));
        }

        #endregion

        #region Tests for LookUpWordAsync

        [TestMethod]
        public async Task LookUpWordAsync_WhenInputDataIsNull_ReturnsBadRequest()
        {
            TranslatorApp.Models.Input.V1.TranslationInput? translationInput = null;

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel> actionResult = await sut.LookUpWordAsync(translationInput!);

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Input data is null");
        }

        [DataTestMethod]
        [DataRow("2")]
        [DataRow("3")]
        public async Task LookUpWordAsync_WhenUnsupportedProtocolVersion_ReturnsBadRequest(string protocolVersion)
        {
            var translationInput = new TranslatorApp.Models.Input.V1.TranslationInput(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: protocolVersion);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel> actionResult = await sut.LookUpWordAsync(translationInput);

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Only protocol version 1 is supported.");
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenModelIsNotValid_ReturnsBadRequest()
        {
            var translationInput = new TranslatorApp.Models.Input.V1.TranslationInput(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: "1");

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SourceLanguage", "SourceLanguage cannot be null or empty"));

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslatorApp.Models.Input.V1.TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslatorApp.Models.Input.V1.TranslationInput>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel> actionResult = await sut.LookUpWordAsync(translationInput);

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Error: SourceLanguage cannot be null or empty.");
        }

        [TestMethod]
        public void LookUpWordAsync_WhenOpenAITranslationServiceThrowsException_LogsError()
        {
            Assert.Inconclusive("This test is not implemented yet.");
        }

        [TestMethod]
        public async Task LookUpWordAsync_Should_ReturnWordModel()
        {
            // Arrange
            var translationInput = new TranslatorApp.Models.Input.V1.TranslationInput(
                Text: "word to translate",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Version: "1");

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslatorApp.Models.Input.V1.TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslatorApp.Models.Input.V1.TranslationInput>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                new ValidationResult());

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel> actionResult = await sut.LookUpWordAsync(translationInput);

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
        }

        #endregion

        #region Tests for FormatValidationErrorMessage

        [TestMethod]
        public void FormatValidationErrorMessage_WhenOneError_AddsFullStopAtTheEnd()
        {
            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("property1", "property1 cannot be null"));

            var sut = _fixture.Create<TranslationController>();
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

            var sut = _fixture.Create<TranslationController>();
            string result = sut.FormatValidationErrorMessage(validationResult);

            result.Should().Be("Error: property1 cannot be null, property2 cannot be null.");
        }

        #endregion
    }
}
