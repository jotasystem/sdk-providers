using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using JotaSystem.Sdk.Providers.Common;

namespace JotaSystem.Sdk.Providers.Tests.Common
{
    public class ProviderBaseTest
    {
        private class FakeProvider : ProviderBase
        {
            public FakeProvider(HttpClient client) : base(client) { }

            public Task<ApiResponse<T>> SendFakeAsync<T>(HttpMethod method,
                                                         string url,
                                                         object? body = null,
                                                         Dictionary<string, string>? headers = null,
                                                         Dictionary<string, string>? queryParams = null,
                                                         string contentType = "application/json",
                                                         TimeSpan? timeout = null)
            {
                return SendRequestAsync<T>(method, url, body, headers, queryParams, contentType, timeout);
            }
        }

        private class TestResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        [Fact]
        public async Task SendRequestAsync_Get_Success()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"message\":\"ok\"}", Encoding.UTF8, "application/json")
                });

            var client = new HttpClient(handler.Object);
            var provider = new FakeProvider(client);

            var result = await provider.SendFakeAsync<TestResponse>(HttpMethod.Get, "https://fake");

            Assert.True(result.Success);
            Assert.Equal("ok", result.Data?.Message);
        }

        [Fact]
        public async Task SendRequestAsync_Get_HttpError()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Erro de requisição")
                });

            var client = new HttpClient(handler.Object);
            var provider = new FakeProvider(client);

            var result = await provider.SendFakeAsync<TestResponse>(HttpMethod.Get, "https://fake");

            Assert.False(result.Success);
            Assert.Contains("Erro", result.ErrorMessage);
        }

        [Fact]
        public async Task SendRequestAsync_Post_WithBodyAndHeaders()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.Content != null &&
                        req.Headers.Contains("X-Test-Header")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"message\":\"created\"}", Encoding.UTF8, "application/json")
                });

            var client = new HttpClient(handler.Object);
            var provider = new FakeProvider(client);

            var body = new { Name = "João" };
            var headers = new Dictionary<string, string> { { "X-Test-Header", "123" } };

            var result = await provider.SendFakeAsync<TestResponse>(HttpMethod.Post, "https://fake", body, headers);

            Assert.True(result.Success);
            Assert.Equal("created", result.Data?.Message);
        }

        [Fact]
        public async Task SendRequestAsync_WithQueryParameters()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.Query.Contains("param=value")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"message\":\"queryok\"}", Encoding.UTF8, "application/json")
                });

            var client = new HttpClient(handler.Object);
            var provider = new FakeProvider(client);

            var queryParams = new Dictionary<string, string> { { "param", "value" } };

            var result = await provider.SendFakeAsync<TestResponse>(HttpMethod.Get, "https://fake", queryParams: queryParams);

            Assert.True(result.Success);
            Assert.Equal("queryok", result.Data?.Message);
        }

        [Fact]
        public async Task SendRequestAsync_ThrowsException_ReturnsFail()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Rede falhou"));

            var client = new HttpClient(handler.Object);
            var provider = new FakeProvider(client);

            var result = await provider.SendFakeAsync<TestResponse>(HttpMethod.Get, "https://fake");

            Assert.False(result.Success);
            Assert.Contains("Erro inesperado", result.ErrorMessage);
        }
    }
}