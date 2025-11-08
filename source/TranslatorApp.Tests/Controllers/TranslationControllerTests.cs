// Ignore Spelling: Deserialize App Validator

using System.Net;
using AutoFixture;
using CopyWords.Parsers;
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
        [DataRow("0")]
        [DataRow("3")]
        public async Task LookUpWordAsync_WhenUnsupportedProtocolVersion_ReturnsBadRequest(string protocolVersion)
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: protocolVersion);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            var result = actionResult.Result as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            result.Value.Should().Be("Only protocol version 1 and 2 are supported.");
        }

        [TestMethod]
        public async Task NormalizeSoundAsync_WhenProtocolVersion1_DoesNotCheckCode()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: "1");

            // Assert
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "invalid-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenProtocolVersion1AndCodeIsNull_ReturnsWordModel()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: "1");

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, null);

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
        }

        [TestMethod]
        public async Task NormalizeSoundAsync_WhenProtocolVersion2AndCodeIsInvalid_ReturnsUnauthorized()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: "2");

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest!, "invalid-code");

            // Assert
            var result = actionResult.Result as UnauthorizedResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenModelIsNotValid_ReturnsBadRequest()
        {
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Version: "1");

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
                DestinationLanguage: "ru",
                Version: "1");

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationController>>>();

            var translateServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translateServiceMock.Setup(x => x.TranslateAsync(It.IsAny<WordModel>())).ThrowsAsync(new Exception("exception from unit test"));

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
        public async Task LookUpWordAsync_WhenWordHasDanishSymbols_CallsDanishParser()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "Løb",
                SourceLanguage: SourceLanguage.Spanish.ToString(), // User probably forgot to switch the dictionary in the app, but the text has Danish characters
                DestinationLanguage: "ru",
                Version: "1");

            WordModel wordModel = _fixture.Create<WordModel>();
            wordModel = wordModel with { SourceLanguage = SourceLanguage.Danish };

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock.Setup(x => x.CheckLanguageSpecificCharacters(It.IsAny<string>())).Returns((true, SourceLanguage.Danish.ToString()));

            var lookUpWordMock = _fixture.Freeze<Mock<ILookUpWord>>();
            lookUpWordMock.Setup(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Danish.ToString())).ReturnsAsync(wordModel);

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();

            lookUpWordMock.Verify(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Danish.ToString()));
            translationsServiceMock.Verify(x => x.TranslateAsync(wordModel));
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenWordHasSpanishSymbols_CallsSpanishParser()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "café",
                SourceLanguage: SourceLanguage.Danish.ToString(), // User probably forgot to switch the dictionary in the app, but the text has Danish characters
                DestinationLanguage: "ru",
                Version: "1");

            WordModel wordModel = _fixture.Create<WordModel>();
            wordModel = wordModel with { SourceLanguage = SourceLanguage.Spanish };

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock.Setup(x => x.CheckLanguageSpecificCharacters(It.IsAny<string>())).Returns((true, SourceLanguage.Spanish.ToString()));

            var lookUpWordMock = _fixture.Freeze<Mock<ILookUpWord>>();
            lookUpWordMock.Setup(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Spanish.ToString())).ReturnsAsync(wordModel);

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();

            lookUpWordMock.Verify(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Spanish.ToString()));
            translationsServiceMock.Verify(x => x.TranslateAsync(wordModel));
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenSourceLanguageIsDanishAndWordsStartsWithAt_RemovesAtAndLooksUpAgain()
        {
            // DDO returns "not found" when search starts with "at ", will should again searching with "at " removed.
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "at ligge",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru",
                Version: "1");

            WordModel wordModel = _fixture.Create<WordModel>();
            wordModel = wordModel with { SourceLanguage = SourceLanguage.Danish };

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock.Setup(x => x.CheckLanguageSpecificCharacters(It.IsAny<string>())).Returns((false, string.Empty));

            var lookUpWordMock = _fixture.Freeze<Mock<ILookUpWord>>();
            lookUpWordMock.Setup(x => x.LookUpWordAsync("at ligge", SourceLanguage.Danish.ToString())).ReturnsAsync((WordModel?)null);
            lookUpWordMock.Setup(x => x.LookUpWordAsync("ligge", SourceLanguage.Danish.ToString())).ReturnsAsync(wordModel);

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
            WordModel returnWordModel = actionResult.Value!;

            lookUpWordMock.Verify(x => x.LookUpWordAsync(It.IsAny<string>(), SourceLanguage.Danish.ToString()), Times.Exactly(2));
            lookUpWordMock.Verify(x => x.LookUpWordAsync(It.IsAny<string>(), SourceLanguage.Spanish.ToString()), Times.Never);
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenCannotFindWordInOnlineDictionary_TriesAnotherParser()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru",
                Version: "1");

            WordModel wordModel = _fixture.Create<WordModel>();
            wordModel = wordModel with { SourceLanguage = SourceLanguage.Spanish };

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock.Setup(x => x.CheckLanguageSpecificCharacters(It.IsAny<string>())).Returns((false, string.Empty));

            var lookUpWordMock = _fixture.Freeze<Mock<ILookUpWord>>();
            lookUpWordMock.Setup(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Danish.ToString())).ReturnsAsync((WordModel?)null);
            lookUpWordMock.Setup(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Spanish.ToString())).ReturnsAsync(wordModel);

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
            WordModel returnWordModel = actionResult.Value!;
            returnWordModel.SourceLanguage.Should().Be(SourceLanguage.Spanish);

            lookUpWordMock.Verify(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Danish.ToString()));
            lookUpWordMock.Verify(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Spanish.ToString()));
        }

        [TestMethod]
        public async Task LookUpWordAsync_WhenCannotFindWordInAnyOnlineDictionary_ReturnsNotFound()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru",
                Version: "1");

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock.Setup(x => x.CheckLanguageSpecificCharacters(It.IsAny<string>())).Returns((false, string.Empty));

            var lookUpWordMock = _fixture.Freeze<Mock<ILookUpWord>>();
            lookUpWordMock.Setup(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Danish.ToString())).ReturnsAsync((WordModel?)null);
            lookUpWordMock.Setup(x => x.LookUpWordAsync(lookUpWordRequest.Text, SourceLanguage.Spanish.ToString())).ReturnsAsync((WordModel?)null);

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
        public async Task LookUpWordAsync_Should_ReturnWordModel()
        {
            // Arrange
            var lookUpWordRequest = new LookUpWordRequest(
                Text: "word to translate",
                SourceLanguage: SourceLanguage.Danish.ToString(),
                DestinationLanguage: "ru",
                Version: "1");

            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock.Setup(x => x.CheckLanguageSpecificCharacters(It.IsAny<string>())).Returns((false, string.Empty));

            var lookUpWordMock = _fixture.Freeze<Mock<ILookUpWord>>();

            // Act
            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> actionResult = await sut.LookUpWordAsync(lookUpWordRequest, "test-code");

            // Assert
            actionResult.Value.Should().BeOfType<WordModel>();
            lookUpWordMock.Verify(x => x.LookUpWordAsync(lookUpWordRequest.Text, lookUpWordRequest.SourceLanguage));
        }

        #endregion
    }
}
