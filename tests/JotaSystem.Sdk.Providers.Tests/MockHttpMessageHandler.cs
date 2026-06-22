using System.Net;
using System.Net.Http.Headers;

namespace JotaSystem.Sdk.Providers.Tests
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;

        public Uri? LastRequestUri { get; private set; }

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _response = response;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            var message = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response)
            };
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Task.FromResult(message);
        }
    }
}
