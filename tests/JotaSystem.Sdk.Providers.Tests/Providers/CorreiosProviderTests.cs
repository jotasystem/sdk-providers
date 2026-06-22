using JotaSystem.Sdk.Providers.Logistics.Shipping.Correios;
using JotaSystem.Sdk.Providers.Logistics.Shipping.Correios.Models;
using System.Net;
using System.Text;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class CorreiosProviderTests
    {
        [Fact]
        public async Task CalculateShippingAsync_Should_Use_Default_Credentials_And_Return_Quote()
        {
            var tokenHandler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "token": "default-token",
                    "expiraEm": "2099-01-01T00:00:00-03:00"
                }
                """));
            var priceHandler = new RecordingHttpMessageHandler(
                CreateResponse("""
                [
                    {
                        "coProduto": "03220",
                        "nuRequisicao": "1",
                        "cepOrigem": "01001000",
                        "cepDestino": "15050305",
                        "psCobrado": "300",
                        "pcFinal": "24,93",
                        "vlTotal": "24,93",
                        "txErro": ""
                    }
                ]
                """));
            var provider = CreateProvider(
                tokenHandler,
                priceHandler,
                new CorreiosCredentials
                {
                    UserName = "software-house",
                    AccessCode = "codigo-acesso",
                    PostingCardNumber = "1234567890"
                });

            var result = await provider.CalculateShippingAsync(
                CreateShippingRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var quote = Assert.Single(result.Data!);
            Assert.Equal("24,93", quote.TotalPrice);
            Assert.Equal("Basic", tokenHandler.Requests[0].AuthorizationScheme);
            Assert.Contains("\"numero\":\"1234567890\"", tokenHandler.Requests[0].Content);
            Assert.Equal("Bearer", priceHandler.Requests[0].AuthorizationScheme);
            Assert.Equal("default-token", priceHandler.Requests[0].AuthorizationParameter);
            Assert.Contains("\"cepOrigem\":\"01001000\"", priceHandler.Requests[0].Content);
        }

        [Fact]
        public async Task CalculateShippingAsync_Should_Prioritize_Provided_Credentials()
        {
            var tokenHandler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "token": "company-token",
                    "expiraEm": "2099-01-01T00:00:00-03:00"
                }
                """));
            var priceHandler = new RecordingHttpMessageHandler(CreateResponse("[]"));
            var provider = CreateProvider(
                tokenHandler,
                priceHandler,
                new CorreiosCredentials
                {
                    UserName = "software-house",
                    AccessCode = "software-code",
                    PostingCardNumber = "111"
                });
            var companyCredentials = new CorreiosCredentials
            {
                UserName = "company",
                AccessCode = "company-code",
                PostingCardNumber = "222"
            };

            var result = await provider.CalculateShippingAsync(
                CreateShippingRequest(),
                companyCredentials,
                TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var expectedBasic = Convert.ToBase64String(Encoding.UTF8.GetBytes("company:company-code"));
            Assert.Equal(expectedBasic, tokenHandler.Requests[0].AuthorizationParameter);
            Assert.Contains("\"numero\":\"222\"", tokenHandler.Requests[0].Content);
        }

        [Fact]
        public async Task CalculateShippingAsync_Should_Reuse_Token_For_Same_Credentials()
        {
            var tokenHandler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "token": "cached-token",
                    "expiraEm": "2099-01-01T00:00:00-03:00"
                }
                """));
            var priceHandler = new RecordingHttpMessageHandler(
                CreateResponse("[]"),
                CreateResponse("[]"));
            var provider = CreateProvider(
                tokenHandler,
                priceHandler,
                new CorreiosCredentials
                {
                    UserName = "user",
                    AccessCode = "code",
                    PostingCardNumber = "333"
                });

            await provider.CalculateShippingAsync(
                CreateShippingRequest(),
                cancellationToken: TestContext.Current.CancellationToken);
            await provider.CalculateShippingAsync(
                CreateShippingRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Single(tokenHandler.Requests);
            Assert.Equal(2, priceHandler.Requests.Count);
        }

        private static CorreiosProvider CreateProvider(
            HttpMessageHandler tokenHandler,
            HttpMessageHandler priceHandler,
            CorreiosCredentials? defaultCredentials)
        {
            var factory = new TestHttpClientFactory(new Dictionary<string, HttpClient>
            {
                [CorreiosHttpClientNames.Token] = new(tokenHandler)
                {
                    BaseAddress = new Uri("https://api.correios.com.br/token/v1/")
                },
                [CorreiosHttpClientNames.Price] = new(priceHandler)
                {
                    BaseAddress = new Uri("https://api.correios.com.br/preco/v1/")
                }
            });

            return new CorreiosProvider(
                factory,
                new CorreiosOptions { DefaultCredentials = defaultCredentials },
                new CorreiosTokenCache());
        }

        private static CorreiosShippingRequest CreateShippingRequest() =>
            new()
            {
                BatchId = "1",
                Products =
                [
                    new CorreiosShippingProductRequest
                    {
                        ProductCode = "03220",
                        OriginZipCode = "01001-000",
                        DestinationZipCode = "15050-305",
                        Weight = "300",
                        Length = "20",
                        Width = "15",
                        Height = "10"
                    }
                ]
            };

        private static HttpResponseMessage CreateResponse(
            string content,
            HttpStatusCode statusCode = HttpStatusCode.OK) =>
            new(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

        private sealed class TestHttpClientFactory(Dictionary<string, HttpClient> clients) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => clients[name];
        }

        private sealed class RecordingHttpMessageHandler(params HttpResponseMessage[] responses)
            : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new(responses);

            public List<RecordedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new RecordedRequest(
                    request.Headers.Authorization?.Scheme,
                    request.Headers.Authorization?.Parameter,
                    request.Content is null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync(cancellationToken)));

                return _responses.Dequeue();
            }
        }

        private sealed record RecordedRequest(
            string? AuthorizationScheme,
            string? AuthorizationParameter,
            string Content);
    }
}
