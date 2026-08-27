// Ignore Spelling: Deserialize App Validator

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
        private Mock<IValidator<SuggestionsRequest>> _suggestionsRequestValidatorMock = default!;
        private Mock<IValidator<WordModel>> _wordModelValidatorMock = default!;

        [TestInitialize]
        public void TestInitialize()
        {
            _fixture = FixtureFactory.CreateWithControllerCustomizations();

            _globalSettingsMock = _fixture.Freeze<Mock<IGlobalSettings>>();
            _globalSettingsMock.Setup(x => x.RequestSecretCode).Returns("test-code");

            _suggestionsRequestValidatorMock = _fixture.Freeze<Mock<IValidator<SuggestionsRequest>>>();
            _suggestionsRequestValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<SuggestionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _wordModelValidatorMock = _fixture.Freeze<Mock<IValidator<WordModel>>>();
            _wordModelValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<WordModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        #region Tests for LookUpWordV3Async

        [TestMethod]
        public async Task LookUpWordV3Async_WhenCodeIsInvalid_ReturnsUnauthorized()
        {
            var sut = _fixture.Create<TranslationController>();

            ActionResult<WordModel?> result = await sut.LookUpWordV3Async(CreateUsableWordModel(), "invalid-code");

            result.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [TestMethod]
        public async Task LookUpWordV3Async_WhenModelIsMissingOrUnusable_ReturnsBadRequest()
        {
            _wordModelValidatorMock
                .Setup(x => x.ValidateAsync(It.IsAny<WordModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Definition.Contexts", "'Definition Contexts' must not be empty.")]));
            var sut = _fixture.Create<TranslationController>();
            WordModel unusable = CreateUsableWordModel() with
            {
                Definition = CreateUsableWordModel().Definition with { Contexts = [] }
            };

            ActionResult<WordModel?> missingResult = await sut.LookUpWordV3Async(null!, "test-code");
            ActionResult<WordModel?> unusableResult = await sut.LookUpWordV3Async(unusable, "test-code");

            missingResult.Result.Should().BeOfType<BadRequestObjectResult>();
            unusableResult.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task LookUpWordV3Async_TranslatesSuppliedModel()
        {
            WordModel suppliedModel = CreateUsableWordModel();
            WordModel translatedModel = suppliedModel with
            {
                Definition = suppliedModel.Definition with
                {
                    Headword = suppliedModel.Definition.Headword with { Translation = "акула" }
                }
            };
            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.TranslateAsync(suppliedModel, "Danish", "Russian", It.IsAny<CancellationToken>()))
                .ReturnsAsync(translatedModel);

            var sut = _fixture.Create<TranslationController>();
            ActionResult<WordModel?> result = await sut.LookUpWordV3Async(suppliedModel, "test-code");

            result.Value.Should().BeSameAs(translatedModel);
            translationsServiceMock.Verify(
                x => x.TranslateAsync(suppliedModel, "Danish", "Russian", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task LookUpWordV3Async_WhenTranslationTimesOut_ReturnsInternalServerError()
        {
            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (WordModel _, string _, string _, CancellationToken cancellationToken) =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return CreateUsableWordModel();
                });
            var sut = _fixture.Create<TranslationController>();
            sut.TranslateRequestTimeout = TimeSpan.FromMilliseconds(10);

            ActionResult<WordModel?> result = await sut.LookUpWordV3Async(CreateUsableWordModel(), "test-code");

            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be("Translation timed out");
        }

        [TestMethod]
        public async Task LookUpWordV3Async_WhenClientCancels_RethrowsCancellation()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cts.Token));
            var sut = _fixture.Create<TranslationController>();

            Func<Task> action = async () => await sut.LookUpWordV3Async(CreateUsableWordModel(), "test-code", cts.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task LookUpWordV3Async_WhenTranslationFails_ReturnsInternalServerError()
        {
            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.TranslateAsync(It.IsAny<WordModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("test failure"));
            var sut = _fixture.Create<TranslationController>();

            ActionResult<WordModel?> result = await sut.LookUpWordV3Async(CreateUsableWordModel(), "test-code");

            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value!.ToString().Should().StartWith("An internal error occurred. CorrelationId:");
        }

        #endregion

        #region Tests for SuggestedWordsV3Async

        [TestMethod]
        public async Task SuggestedWordsV3Async_WhenCodeIsInvalid_ReturnsUnauthorized()
        {
            var request = new SuggestionsRequest("привет", "Danish");
            var sut = _fixture.Create<TranslationController>();

            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsV3Async(request, "invalid-code");

            actionResult.Result.Should().BeOfType<UnauthorizedResult>();
        }

        [TestMethod]
        public async Task SuggestedWordsV3Async_WhenRequestIsInvalid_ReturnsBadRequest()
        {
            var request = new SuggestionsRequest("привет", "English");
            var validationResult = new ValidationResult([new ValidationFailure("DestinationLanguage", "Unsupported language")]);
            _suggestionsRequestValidatorMock
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
            var sut = _fixture.Create<TranslationController>();

            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsV3Async(request, "test-code");

            actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SuggestedWordsV3Async_WhenRequestIsValid_ReturnsAISuggestions()
        {
            var request = new SuggestionsRequest("привет", "Spanish");
            var translationsServiceMock = _fixture.Freeze<Mock<ITranslationsService>>();
            translationsServiceMock
                .Setup(x => x.GetAISuggestedWordsAsync(request.Text, request.DestinationLanguage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(["hola", "buenas"]);
            var sut = _fixture.Create<TranslationController>();

            ActionResult<SuggestedWordsModel> actionResult = await sut.SuggestedWordsV3Async(request, "test-code");

            var result = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
            var model = result.Value.Should().BeOfType<SuggestedWordsModel>().Subject;
            model.Words.Should().Equal("hola", "buenas");
            translationsServiceMock.Verify(
                x => x.GetAISuggestedWordsAsync(request.Text, request.DestinationLanguage, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        private static WordModel CreateUsableWordModel()
        {
            var definition = new Definition(
                new Headword("haj", null, null),
                "substantiv, fælleskøn",
                "-en, -er, -erne",
                [new Context("", "", [new Meaning("stor bruskfisk", null, "1", null, null, null, [])])]);

            return new WordModel("haj", SourceLanguage.Danish, null, null, definition, [], []);
        }
    }
}
