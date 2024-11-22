using System.Collections.Specialized;
using System.Net;
using AutoFixture;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using My.Function;
using TranslationsFunc.Models;
using TranslationsFunc.Services;

namespace TranslationFunc.Tests
{
    [TestClass]
    public class HttpTranslateTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

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
        public async Task Run_WhenQueryParameterServiceIsAzure_CallsAzureTranslationService()
        {
            TranslationInput translationInput = new TranslationInput("da", ["ru", "en"], "word to translate", "meaning", "noun", []);

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
            TranslationInput translationInput = new TranslationInput("da", ["ru", "en"], "word to translate", "meaning", "noun", []);
            var httpRequestData = MockHttpRequestData.Create(translationInput, []);
            var translationOutput = _fixture.Create<TranslationOutput>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();
            openAITranslationServiceMock.Setup(x => x.TranslateAsync(It.IsAny<TranslationInput>())).ReturnsAsync(translationOutput);

            var sut = _fixture.Create<HttpTranslate>();
            HttpResponseData result = await sut.Run(httpRequestData);

            result.StatusCode.Should().Be(HttpStatusCode.OK);

            openAITranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslationInput>()));
        }
    }
}
