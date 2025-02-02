using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;
using System.Collections.Specialized;

namespace TranslationFunc.Tests
{
    // see this example: https://stackoverflow.com/a/76396639
    public static class MockHttpRequestData
    {
        public static HttpRequestData Create()
        {
            return Create("", []);
        }

        public static HttpRequestData Create<T>(T? requestData, NameValueCollection query) where T : class
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddFunctionsWorkerDefaults();

            MemoryStream bodyDataStream;
            if (requestData is null)
            {
                bodyDataStream = new MemoryStream(0);
            }
            else
            {
                string serializedData = JsonSerializer.Serialize(requestData);
                bodyDataStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedData));
            }

            var context = new Mock<FunctionContext>();
            context.SetupProperty(context => context.InstanceServices, serviceCollection.BuildServiceProvider());

            var request = new Mock<HttpRequestData>(context.Object);
            request.Setup(r => r.Body).Returns(bodyDataStream);
            request.Setup(r => r.CreateResponse()).Returns(new MockHttpResponseData(context.Object));
            request.Setup(r => r.Query).Returns(query);

            return request.Object;
        }

        public class MockHttpResponseData : HttpResponseData
        {
            public MockHttpResponseData(FunctionContext functionContext)
                : base(functionContext)
            {
            }

            public override HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

            public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();

            public override Stream Body { get; set; } = new MemoryStream();

            public override HttpCookies Cookies { get; } = default!;
        }
    }
}
