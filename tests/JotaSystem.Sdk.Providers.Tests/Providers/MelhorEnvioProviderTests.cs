using JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio;
using JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio.Models;
using System.Net;
using System.Text;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class MelhorEnvioProviderTests
    {
        [Fact]
        public async Task CalculateShippingAsync_Should_Use_Default_Token_And_Return_Quotes()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                [
                    {
                        "id": 1,
                        "name": "PAC",
                        "price": "37.79",
                        "custom_price": "39.79",
                        "discount": "2.09",
                        "currency": "R$",
                        "delivery_time": 9,
                        "delivery_range": { "min": 8, "max": 9 },
                        "custom_delivery_time": 10,
                        "custom_delivery_range": { "min": 9, "max": 10 },
                        "packages": [
                            {
                                "price": "37.79",
                                "discount": "2.09",
                                "format": "box",
                                "dimensions": { "height": 2, "width": 11, "length": 16 },
                                "weight": "0.10",
                                "insurance_value": "50.00",
                                "products": [{ "id": "produto-a", "quantity": 1 }]
                            }
                        ],
                        "additional_services": {
                            "receipt": true,
                            "own_hand": false,
                            "collect": false
                        },
                        "company": {
                            "id": 1,
                            "name": "Correios",
                            "picture": "https://sandbox.melhorenvio.com.br/images/shipping-companies/correios.png"
                        }
                    }
                ]
                """));
            var provider = CreateProvider(handler, "default-token");

            var result = await provider.CalculateShippingAsync(
                CreateProductQuoteRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var quote = Assert.Single(result.Data!);
            Assert.Equal("PAC", quote.Name);
            Assert.Equal("39.79", quote.CustomPrice);
            Assert.Equal(10, quote.CustomDeliveryTime);
            Assert.Equal("Correios", quote.Company!.Name);
            Assert.Equal("Bearer", handler.Requests[0].AuthorizationScheme);
            Assert.Equal("default-token", handler.Requests[0].AuthorizationParameter);
            Assert.Equal("JotaSystemSdk/1.0", handler.Requests[0].UserAgent);
            Assert.Equal("application/json", handler.Requests[0].Accept);
            Assert.Equal("/api/v2/me/shipment/calculate", handler.Requests[0].Path);
            Assert.Contains("\"postal_code\":\"01001000\"", handler.Requests[0].Content);
            Assert.Contains("\"postal_code\":\"15050305\"", handler.Requests[0].Content);
            Assert.Contains("\"insurance_value\":10.10", handler.Requests[0].Content);
            Assert.Contains("\"services\":\"1,2,18\"", handler.Requests[0].Content);
        }

        [Fact]
        public async Task CalculateShippingAsync_Should_Prioritize_Provided_Token()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("[]"));
            var provider = CreateProvider(handler, "default-token");

            var result = await provider.CalculateShippingAsync(
                CreateProductQuoteRequest(),
                "company-token",
                TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal("company-token", handler.Requests[0].AuthorizationParameter);
        }

        [Fact]
        public async Task CalculateShippingAsync_Should_Send_Volumes()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("[]"));
            var provider = CreateProvider(handler, "default-token");

            var result = await provider.CalculateShippingAsync(
                CreateVolumeQuoteRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Contains("\"volumes\":[", handler.Requests[0].Content);
            Assert.Contains("\"insurance\":10.10", handler.Requests[0].Content);
        }

        [Fact]
        public async Task CalculateShippingAsync_Should_Validate_Required_Data()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("[]"));
            var provider = CreateProvider(handler, "default-token");

            var result = await provider.CalculateShippingAsync(
                new MelhorEnvioShippingQuoteRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Equal("CEP de origem deve conter 8 digitos.", result.ErrorMessage);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CalculateShippingAsync_Should_Return_Api_Error_Content()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "message": "The given data was invalid.",
                    "errors": {
                        "from.postal_code": ["O campo from.postal code e obrigatorio."]
                    }
                }
                """, HttpStatusCode.UnprocessableEntity));
            var provider = CreateProvider(handler, "default-token");

            var result = await provider.CalculateShippingAsync(
                CreateProductQuoteRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Contains("The given data was invalid.", result.ErrorMessage);
        }

        private static MelhorEnvioProvider CreateProvider(HttpMessageHandler handler, string? defaultToken)
        {
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(MelhorEnvioEnvironment.SandboxBaseUrl)
            };

            return new MelhorEnvioProvider(
                client,
                new MelhorEnvioOptions
                {
                    AccessToken = defaultToken,
                    UserAgent = "JotaSystemSdk/1.0"
                });
        }

        private static MelhorEnvioShippingQuoteRequest CreateProductQuoteRequest() =>
            new()
            {
                From = new MelhorEnvioShippingLocationRequest { PostalCode = "01001-000" },
                To = new MelhorEnvioShippingLocationRequest { PostalCode = "15050-305" },
                Services = "1,2,18",
                Options = new MelhorEnvioShippingOptionsRequest
                {
                    Receipt = false,
                    OwnHand = false
                },
                Products =
                [
                    new MelhorEnvioShippingProductRequest
                    {
                        Id = "produto-a",
                        Width = 11,
                        Height = 17,
                        Length = 11,
                        Weight = 1,
                        InsuranceValue = 10.10m,
                        Quantity = 1
                    }
                ]
            };

        private static MelhorEnvioShippingQuoteRequest CreateVolumeQuoteRequest() =>
            new()
            {
                From = new MelhorEnvioShippingLocationRequest { PostalCode = "01001-000" },
                To = new MelhorEnvioShippingLocationRequest { PostalCode = "15050-305" },
                Volumes =
                [
                    new MelhorEnvioShippingVolumeRequest
                    {
                        Width = 11,
                        Height = 17,
                        Length = 11,
                        Weight = 0.3m,
                        Insurance = 10.10m
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
                    request.RequestUri?.ToString() ?? string.Empty,
                    request.RequestUri?.AbsolutePath ?? string.Empty,
                    request.Headers.Authorization?.Scheme,
                    request.Headers.Authorization?.Parameter,
                    request.Headers.UserAgent.ToString(),
                    request.Headers.Accept.ToString(),
                    request.Content is null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync(cancellationToken)));

                return _responses.Dequeue();
            }
        }

        private sealed record RecordedRequest(
            string Url,
            string Path,
            string? AuthorizationScheme,
            string? AuthorizationParameter,
            string UserAgent,
            string Accept,
            string Content);
    }
}
