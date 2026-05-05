// Ignore Spelling: Deserialize App Validator

using System.Net;
using AutoFixture;
using CopyWords.Parsers.Exceptions;
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
        private static IReadOnlyList<string> ActiveDictionaries(params string[] values) => values;

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
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

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
        public async Task LookUpWordAsync_WhenDdoDictionaryIsUnavailable_ReturnsServiceUnavailableWithDictionaryNameAndOriginalError()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "islygte",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            string originalError = "Server returned error code 'ServiceUnavailable' when requesting URL 'https://ordnet.dk/ddo/ordbog?query=islygte'.";

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServerErrorException(
                    originalError,
                    HttpStatusCode.ServiceUnavailable,
                    "https://ordnet.dk/ddo/ordbog?query=islygte"));

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            var result = actionResult.Result as ObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            result.Value.Should().BeOfType<string>();
            result.Value!.ToString().Should().Contain("Online dictionary 'DDO' is temporarily unavailable.");
            result.Value!.ToString().Should().Contain(originalError);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == (int)TranslatorAppEventId.OnlineDictionaryUnavailable),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("Online dictionary 'DDO' is temporarily unavailable.")
                        && v.ToString()!.Contains(originalError)),
                    It.IsAny<ServerErrorException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenSpanishDictIsUnavailable_ReturnsServiceUnavailableWithDictionaryNameAndOriginalError()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "afeitar",
                SourceLanguage: SourceLanguage.Spanish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Spanish.ToString()));

            string originalError = "Server returned error code 'ServiceUnavailable' when requesting URL 'https://www.spanishdict.com/translate/afeitar'.";

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ServerErrorException(
                    originalError,
                    HttpStatusCode.ServiceUnavailable,
                    "https://www.spanishdict.com/translate/afeitar"));

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            var result = actionResult.Result as ObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            result.Value.Should().BeOfType<string>();
            result.Value!.ToString().Should().Contain("Online dictionary 'SpanishDict' is temporarily unavailable.");
            result.Value!.ToString().Should().Contain(originalError);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == (int)TranslatorAppEventId.OnlineDictionaryUnavailable),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("Online dictionary 'SpanishDict' is temporarily unavailable.")
                        && v.ToString()!.Contains(originalError)),
                    It.IsAny<ServerErrorException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenTranslateServiceThrowsException_LogsErrorAndReturnsInternalServerError()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translateServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translateServiceMock
                .Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("exception from unit test"));

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            var result = actionResult.Result as ObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(500);
            result.Value.Should().BeOfType<string>();
            result.Value!.ToString().Should().Contain("An internal error occurred. CorrelationId:");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred while trying to look up the word.") && v.ToString()!.Contains("CorrelationId:")),
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
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
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
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            WordModel wordModel = _fixture.Create<WordModel>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
            translationsServiceMock.Verify(x => x.LookUpWordInDictionaryAsync(lookUpWordRequest.Text, lookUpWordRequest.SourceLanguage, lookUpWordRequest.DestinationLanguage, lookUpWordRequest.ActiveDictionaries, It.IsAny<CancellationToken>()));

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.Is<EventId>(e => e.Id == (int)TranslatorAppEventId.ReturningWordModel2),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Returning word model")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenRequestIsValid_LogsLookupRequestParameters()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            WordModel wordModel = _fixture.Create<WordModel>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var sut = _fixture.Create<TranslationController>();
            await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.Is<EventId>(e => e.Id == (int)TranslatorAppEventId.LookupControllerRequestReceived),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("TranslationController received lookup request.")
                        && v.ToString()!.Contains(lookUpWordRequest.Text)
                        && v.ToString()!.Contains(lookUpWordRequest.SourceLanguage)
                        && v.ToString()!.Contains(lookUpWordRequest.DestinationLanguage)
                        && v.ToString()!.Contains(SourceLanguage.Danish.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenLookupTimesOut_ReturnsInternalServerErrorAndLogsWarning()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async (string text, string source, string dest, IReadOnlyList<string> activeDictionaries, CancellationToken ct) =>
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
            result!.StatusCode.Should().Be(500);
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
        public async Task LookUpWordAsync_WhenTranslateTimesOut_ReturnsInternalServerErrorAndLogsWarning()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            WordModel wordModel = _fixture.Create<WordModel>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);
            translationsServiceMock
                .Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (WordModel wm, string src, string dest, CancellationToken ct) =>
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
            result!.StatusCode.Should().Be(500);
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
                DestinationLanguage: "de",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var sut = _fixture.Create<TranslationController>();

            // Act & Assert
            await sut.Invoking(x => x.LookUpWordAsync(lookUpWordRequest, "test-code", cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenMeaningLookupUrlForSpanishWord_ShouldTranslateFromEnglish()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "https://www.spanishdict.com/translate/hello?langFrom=en",
                SourceLanguage: "Spanish",
                DestinationLanguage: "Russian",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Spanish.ToString()));

            var wordModel = _fixture.Build<WordModel>()
                .With(x => x.SourceLanguage, SourceLanguage.Spanish)
                .Create();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);

            var sut = _fixture.Create<TranslationController>();

            // Act
            await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert - verify TranslateAsync was called with "English" as source language instead of "Spanish"
            translationsServiceMock.Verify(
                x => x.TranslateAsync(
                    It.IsAny<WordModel>(),
                    "English",
                    lookUpWordRequest.DestinationLanguage,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenActiveDictionariesProvided_ForwardsThemToTranslationsService()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "de",
                ActiveDictionaries: [SourceLanguage.Danish.ToString()]);

            WordModel wordModel = _fixture.Create<WordModel>();

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.LookUpWordInDictionaryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(wordModel);

            var sut = _fixture.Create<TranslationController>();
            await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            translationsServiceMock.Verify(
                x => x.LookUpWordInDictionaryAsync(
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage,
                    lookUpWordRequest.DestinationLanguage,
                    lookUpWordRequest.ActiveDictionaries,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Tests for SuggestedWordsAsync

        [TestMethod]
        public async Task SuggestedWordsAsync_WhenInputDataIsNull_ReturnsBadRequest()
        {
            LookUpWordRequest? lookUpWordRequest = null;

            var sut = _fixture.Create<TranslationController>();
            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsAsync(lookUpWordRequest!, "test-code");

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("Input data is null");
        }

        [TestMethod]
        public async Task SuggestedWordsAsync_WhenCodeIsInvalid_ReturnsUnauthorized()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "xyz",
                SourceLanguage: "Spanish",
                DestinationLanguage: "Russian",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Spanish.ToString()));

            var sut = _fixture.Create<TranslationController>();
            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsAsync(lookUpWordRequest, "invalid-code");

            var result = actionResult.Result as UnauthorizedResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task SuggestedWordsAsync_WhenModelIsNotValid_ReturnsBadRequest()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "xyz",
                SourceLanguage: "",
                DestinationLanguage: "Russian",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("SourceLanguage", "SourceLanguage cannot be null or empty"));
            _requestValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<LookUpWordRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsAsync(lookUpWordRequest, "test-code");

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            result.Value.Should().Be("Error: SourceLanguage cannot be null or empty.");
        }

        [TestMethod]
        public async Task SuggestedWordsAsync_WhenRequestIsValid_ReturnsSuggestedWordsModel()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "xyz",
                SourceLanguage: "Spanish",
                DestinationLanguage: "Russian",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Spanish.ToString()));

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.GetSuggestedWordsAsync(lookUpWordRequest.Text, lookUpWordRequest.SourceLanguage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(["uno", "dos"]);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsAsync(lookUpWordRequest, "test-code");

            var result = actionResult.Result as OkObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.OK);

            result.Value.Should().BeOfType<SuggestedWordsModel>();
            var model = (SuggestedWordsModel)result.Value!;
            model.Words.Should().Equal("uno", "dos");
        }

        [TestMethod]
        public async Task SuggestedWordsAsync_WhenSourceLanguageIsDanish_UsesTranslationsServiceSuggestions()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "islygte",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "Russian",
                ActiveDictionaries: ActiveDictionaries(SourceLanguage.Danish.ToString()));

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.GetSuggestedWordsAsync(lookUpWordRequest.Text, lookUpWordRequest.SourceLanguage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(["lygte", "flygte"]);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsAsync(lookUpWordRequest, "test-code");

            var result = actionResult.Result as OkObjectResult;
            result.Should().NotBeNull();

            var model = (SuggestedWordsModel)result!.Value!;
            model.Words.Should().Equal("lygte", "flygte");

            translationsServiceMock.Verify(
                x => x.GetSuggestedWordsAsync(
                    lookUpWordRequest.Text,
                    lookUpWordRequest.SourceLanguage,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
