// Ignore Spelling: Deserialize

using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker.Http;
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

        [TestMethod]
        public async Task Run_WhenCanDeserializeAsInputObject_ReturnsTranslationOutput()
        {
            var translationInput = new TranslationInput(
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word to translate",
                Meaning: "meaning",
                PartOfSpeech: "noun",
                Examples: []);

            NameValueCollection query = new();
            query["service"] = "azure";

            var httpRequestData = MockHttpRequestData.Create(translationInput, query);
            var translationOutput = _fixture.Create<TranslationOutput>();

            var azureTranslationServiceMock = _fixture.Freeze<Mock<IAzureTranslationService>>();
            azureTranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ReturnsAsync(translationOutput);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();

            var jsonResponse = JsonSerializer.Deserialize<TranslationOutput>(responseBody);
            jsonResponse.Should().BeEquivalentTo(translationOutput);
        }

        [TestMethod]
        public async Task Run_WhenCanDeserializeAsInput2Object_ReturnsTranslationOutput2()
        {
            var translationInput2 = new TranslationInput2(
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

            var httpRequestData = MockHttpRequestData.Create(translationInput2, query);
            var translationOutput2 = _fixture.Create<TranslationOutput2>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.Translate2Async(It.IsAny<TranslationInput2>())).ReturnsAsync(translationOutput2);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();

            var deserializedResponse = JsonSerializer.Deserialize<TranslationOutput2>(responseBody);
            deserializedResponse.Should().BeEquivalentTo(translationOutput2);
        }

        [TestMethod]
        public async Task Run_WhenValidationDoesNotPassForInput_ReturnsBadRequest()
        {
            TranslationInput translationInput = new TranslationInput(
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word to translate",
                Meaning: "meaning",
                PartOfSpeech: "noun",
                Examples: []);

            var httpRequestData = MockHttpRequestData.Create(translationInput, []);

            var validationResult = _fixture.Create<ValidationResult>();
            validationResult.Errors.Clear();
            validationResult.Errors.Add(new ValidationFailure("property1", "property1 cannot be null"));

            var translationInputValidatorMock = _fixture.Freeze<Mock<IValidator<TranslationInput>>>();
            translationInputValidatorMock.Setup(x => x.ValidateAsync(It.IsAny<TranslationInput>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            result.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(result.Body);
            string responseBody = await streamReader.ReadToEndAsync();
            responseBody.Should().Be("{\"Error\":\"Error: property1 cannot be null.\"}");
        }

        [TestMethod]
        public async Task Run_WhenQueryParameterServiceIsAzure_CallsAzureTranslationService()
        {
            TranslationInput translationInput = new TranslationInput(
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word to translate",
                Meaning: "meaning",
                PartOfSpeech: "noun",
                Examples: []);

            NameValueCollection query = new();
            query["service"] = "azure";

            var httpRequestData = MockHttpRequestData.Create(translationInput, query);
            var translationOutput = _fixture.Create<TranslationOutput>();

            var azureTranslationServiceMock = _fixture.Freeze<Mock<IAzureTranslationService>>();
            azureTranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ReturnsAsync(translationOutput);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            azureTranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslationInput>()));
        }

        [TestMethod]
        public async Task Run_WhenQueryParameterServiceIsNotAzure_CallsOpenAITranslationService()
        {
            TranslationInput translationInput = new TranslationInput(
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word to translate",
                Meaning: "meaning",
                PartOfSpeech: "noun",
                Examples: []);
            var httpRequestData = MockHttpRequestData.Create(translationInput, []);
            var translationOutput = _fixture.Create<TranslationOutput>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ReturnsAsync(translationOutput);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            openAITranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslationInput>()));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task Run_WhenExceptionOccurs_LogsError()
        {
            TranslationInput translationInput = new TranslationInput(
                SourceLanguage: "da",
                DestinationLanguages: ["ru", "en"],
                Word: "word to translate",
                Meaning: "meaning",
                PartOfSpeech: "noun",
                Examples: []);
            var httpRequestData = MockHttpRequestData.Create(translationInput, []);
            var translationOutput = _fixture.Create<TranslationOutput>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ThrowsAsync(new Exception("exception from unit test"));

            var sut = _fixture.Create<HttpTranslate>();

            try
            {
                HttpResponseData result = await sut.Run(httpRequestData);
            }
            catch (Exception ex)
            {
                ex.Message.Should().Be("exception from unit test");
                throw;
            }
        }

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
