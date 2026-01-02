// Ignore Spelling: Deserialize App Validator

using System.Net;
using AutoFixture;
using CopyWords.Parsers.Models;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Controllers;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Controllers
{
    [TestClass]
    public class TranslationControllerTests
    {
        private IFixture _fixture = default!;
        private Mock<IGlobalSettings> _globalSettingsMock = default!;
        private Mock<IValidator<LookUpWordRequest>> _requestValidatorMock = default!;

        [TestInitialize]
        public void TestInitialize()
        {
            _fixture = FixtureFactory.CreateWithControllerCustomizations();

            _globalSettingsMock = _fixture.Freeze<Mock<IGlobalSettings>>();
            _globalSettingsMock.Setup(x => x.RequestSecretCode).Returns("test-code");

            _requestValidatorMock = _fixture.Freeze<Mock<IValidator<LookUpWordRequest>>>();
            _requestValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<LookUpWordRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        #region Tests for LookUpWordAsync

        [TestMethod]
        public async Task LookUpWordAsync_WhenInputDataIsNull_ReturnsBadRequest()
        {
            LookUpWordRequest? lookUpWordRequest = null;

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest!, "test-code");

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Input data is null");
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenModelIsNotValid_ReturnsBadRequest()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru");

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SourceLanguage", "SourceLanguage cannot be null or empty"));

            _requestValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<LookUpWordRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Error: SourceLanguage cannot be null or empty.");
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenTranslateServiceThrowsException_LogsError()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translateServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translateServiceMock.Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("exception from unit test"));

            // Act
            var sut = _fixture.Create<TranslationController>();
            await sut.Invoking(x => x.LookUpWordAsync(lookUpWordRequest, "test-code"))
                .Should().ThrowAsync<Exception>()
                .WithMessage("exception from unit test");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred while trying to look up the word.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenCannotFindWordInAnyOnlineDictionary_ReturnsNotFound()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((WordModel?)null);

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            var result = actionResult.Result as NotFoundObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

            result.Value.Should().Be("Word 'word to translate' not found.");
        }

        [TestMethod]
        public async Task LookUpWordAsync_Should_CallTranslationsService()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            WordModel wordModel = _fixture.Create<WordModel>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
            translationsServiceMock.Verify(x => x.LookUpWordInDictionaryAsync(lookUpWordRequest.Text, lookUpWordRequest.SourceLanguage, lookUpWordRequest.DestinationLanguage, It.IsAny<CancellationToken>()));
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenLookupTimesOut_ReturnsGatewayTimeoutAndLogsWarning()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string text, string source, string dest, CancellationToken ct) =>
                {
                    await Task.Delay(200, ct);
                    return _fixture.Create<WordModel>();
                });

            var sut = _fixture.Create<TranslationController>();
            sut.LookupRequestTimeout = TimeSpan.FromMilliseconds(10);

            // Act
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            var result = actionResult.Result as ObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(504);
            result.Value.Should().Be("Translation timed out");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    new EventId((int)TranslatorAppEventId.CallingOnlineDictionaryTimedOut),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Calling online dictionary timed out")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenTranslateTimesOut_ReturnsGatewayTimeoutAndLogsWarning()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            WordModel wordModel = _fixture.Create<WordModel>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);
            translationsServiceMock
                .Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<CancellationToken>()))
                .Returns(async (WordModel wm, CancellationToken ct) =>
                {
                    await Task.Delay(200, ct);
                    return wm;
                });

            var sut = _fixture.Create<TranslationController>();
            sut.TranslateRequestTimeout = TimeSpan.FromMilliseconds(10);

            // Act
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            var result = actionResult.Result as ObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(504);
            result.Value.Should().Be("Translation timed out");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    new EventId((int)TranslatorAppEventId.CallingOpenAITimeoudOut),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Calling OpenAI API timed out")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenClientCancelsRequest_RethrowsOperationCanceledException()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru");

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var sut = _fixture.Create<TranslationController>();

            // Act & Assert
            await sut.Invoking(x => x.LookUpWordAsync(lookUpWordRequest, "test-code", cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion
    }
}
