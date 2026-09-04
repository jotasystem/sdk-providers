using JotaSystem.Sdk.Providers.Payments.Cielo;
using JotaSystem.Sdk.Providers.Payments.Cielo.Models;
using System.Net;
using System.Text;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class CieloProviderTests
    {
        [Fact]
        public async Task CreateSaleAsync_Should_Send_Credentials_And_Pascal_Case_Payload()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "Pedido123",
                    "Payment": {
                        "PaymentId": "6f8d1753-86bb-4dc0-9ebb-09a29093e1fb",
                        "Type": "CreditCard",
                        "Amount": 15700,
                        "Status": 2,
                        "ReturnCode": "6",
                        "ReturnMessage": "Operation Successful"
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.CreateSaleAsync(
                CreateCreditCardSale(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal("6f8d1753-86bb-4dc0-9ebb-09a29093e1fb", result.Data!.Payment.PaymentId);
            Assert.Equal(2, result.Data.Payment.Status);
            Assert.NotNull(result.Data.RawPayload);

            var request = handler.Requests[0];
            Assert.Equal("https://apisandbox.cieloecommerce.cielo.com.br/1/sales/", request.Url);
            Assert.Equal("merchant-id", request.MerchantId);
            Assert.Equal("merchant-key", request.MerchantKey);
            Assert.Contains("\"MerchantOrderId\":\"Pedido123\"", request.Content);
            Assert.Contains("\"Type\":\"CreditCard\"", request.Content);
            Assert.Contains("\"Amount\":15700", request.Content);
        }

        [Fact]
        public async Task CreateSaleAsync_Should_Use_Production_Url_For_Production_Credentials()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"Payment":{"PaymentId":"1","Status":1}}""", HttpStatusCode.Created));
            var credentials = CreateCredentials();
            credentials.Environment = CieloEnvironmentEnum.Production;
            var provider = CreateProvider(handler, credentials);

            await provider.CreateSaleAsync(
                CreateCreditCardSale(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("https://api.cieloecommerce.cielo.com.br/1/sales/", handler.Requests[0].Url);
        }

        [Fact]
        public async Task CreateSaleAsync_Should_Reject_MerchantOrderId_With_Special_Characters()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateProvider(handler, CreateCredentials());
            var sale = CreateCreditCardSale();
            sale.MerchantOrderId = "Pedido/123";

            var result = await provider.CreateSaleAsync(sale, cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Contains("MerchantOrderId", result.ErrorMessage!);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateSaleAsync_Should_Fail_When_Credentials_Are_Missing()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateProvider(handler, defaultCredentials: null);

            var result = await provider.CreateSaleAsync(
                CreateCreditCardSale(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateSaleAsync_Should_Describe_Business_Errors_Returned_By_Cielo()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""[{"Code":126,"Message":"Credit Card Expiration Date is invalid"}]""",
                    HttpStatusCode.BadRequest));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.CreateSaleAsync(
                CreateCreditCardSale(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Equal("126 - Credit Card Expiration Date is invalid", result.ErrorMessage);
        }

        [Fact]
        public async Task CreatePixSaleAsync_Should_Return_QrCode_Data()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "Pedido123",
                    "Payment": {
                        "PaymentId": "1997be4d-694a-472e-98f0-e7f4b4c8f1e7",
                        "Type": "Pix",
                        "QrcodeBase64Image": "cXJjb2Rl",
                        "QrCodeString": "00020101021226880014br.gov.bcb.pix",
                        "Amount": 100,
                        "Status": 12,
                        "ReturnMessage": "Pix gerado com sucesso"
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.CreateSaleAsync(
                new CieloSaleRequest
                {
                    MerchantOrderId = "Pedido123",
                    Payment = new CieloPaymentRequest { Type = CieloPaymentTypes.Pix, Amount = 100 }
                },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal("00020101021226880014br.gov.bcb.pix", result.Data!.Payment.QrCodeString);
            Assert.Equal("cXJjb2Rl", result.Data.Payment.QrCodeBase64Image);
        }

        [Fact]
        public async Task VoidAsync_Should_Send_Amount_On_Query_String()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"Status":11,"ReturnCode":"9","ReturnMessage":"Operation Successful"}"""));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.VoidAsync(
                "payment-id",
                5000,
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal(11, result.Data!.Status);
            Assert.Equal(
                "https://apisandbox.cieloecommerce.cielo.com.br/1/sales/payment-id/void?amount=5000",
                handler.Requests[0].Url);
            Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        }

        [Fact]
        public async Task CaptureAsync_Should_Capture_Full_Amount_When_Value_Is_Not_Informed()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""{"Status":2}"""));
            var provider = CreateProvider(handler, CreateCredentials());

            await provider.CaptureAsync("payment-id", cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(
                "https://apisandbox.cieloecommerce.cielo.com.br/1/sales/payment-id/capture",
                handler.Requests[0].Url);
        }

        [Fact]
        public async Task UpdateRecurrentPaymentAsync_Should_Send_One_Request_Per_Change()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse(string.Empty),
                CreateResponse(string.Empty));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.UpdateRecurrentPaymentAsync(
                "recurrent-id",
                new CieloRecurrentPaymentUpdate
                {
                    Amount = 2500,
                    EndDate = new DateOnly(2030, 12, 31)
                },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal(2, handler.Requests.Count);
            Assert.EndsWith("/1/RecurrentPayment/recurrent-id/Amount", handler.Requests[0].Url);
            Assert.Equal("2500", handler.Requests[0].Content);
            Assert.EndsWith("/1/RecurrentPayment/recurrent-id/EndDate", handler.Requests[1].Url);
            Assert.Equal("\"2030-12-31\"", handler.Requests[1].Content);
        }

        [Fact]
        public async Task UpdateRecurrentPaymentAsync_Should_Fail_When_There_Is_Nothing_To_Change()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.UpdateRecurrentPaymentAsync(
                "recurrent-id",
                new CieloRecurrentPaymentUpdate(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task DeactivateRecurrentPaymentAsync_Should_Call_Deactivate_Resource()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse(string.Empty));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.DeactivateRecurrentPaymentAsync(
                "recurrent-id",
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.EndsWith("/1/RecurrentPayment/recurrent-id/Deactivate", handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetSaleAsync_Should_Use_Query_Host()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"MerchantOrderId":"Pedido123","Payment":{"PaymentId":"payment-id","Status":2}}"""));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.GetSaleAsync(
                "payment-id",
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal(
                "https://apiquerysandbox.cieloecommerce.cielo.com.br/1/sales/payment-id",
                handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetSalesByMerchantOrderIdAsync_Should_Return_Payments()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "Payments": [
                        { "PaymentId": "payment-id", "ReceveidDate": "2024-12-12 09:29:31" }
                    ]
                }
                """));
            var provider = CreateProvider(handler, CreateCredentials());

            var result = await provider.GetSalesByMerchantOrderIdAsync(
                "Pedido123",
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            var payment = Assert.Single(result.Data!);
            Assert.Equal("payment-id", payment.PaymentId);
            Assert.Equal("2024-12-12 09:29:31", payment.ReceivedDate);
        }

        internal static CieloProvider CreateProvider(HttpMessageHandler handler, CieloCredentials? defaultCredentials) =>
            new(new TestHttpClientFactory(new HttpClient(handler)),
                new CieloOptions { DefaultCredentials = defaultCredentials });

        internal static CieloCredentials CreateCredentials() =>
            new() { MerchantId = "merchant-id", MerchantKey = "merchant-key" };

        internal static HttpResponseMessage CreateResponse(
            string content,
            HttpStatusCode statusCode = HttpStatusCode.OK) =>
            new(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

        private static CieloSaleRequest CreateCreditCardSale() =>
            new()
            {
                MerchantOrderId = "Pedido123",
                Customer = new CieloCustomer { Name = "Aline de Souza" },
                Payment = new CieloPaymentRequest
                {
                    Type = CieloPaymentTypes.CreditCard,
                    Amount = 15700,
                    Installments = 1,
                    CreditCard = new CieloCreditCardRequest
                    {
                        CardNumber = "4091688625337641",
                        Holder = "Aline de Souza",
                        ExpirationDate = "12/2035",
                        SecurityCode = "333",
                        Brand = "Visa"
                    }
                }
            };

        internal sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => client;
        }

        internal sealed class RecordingHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new(responses);

            public List<RecordedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri!.ToString(),
                    ReadHeader(request, "MerchantId"),
                    ReadHeader(request, "MerchantKey"),
                    request.Content is null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync(cancellationToken)));

                return _responses.Dequeue();
            }

            private static string? ReadHeader(HttpRequestMessage request, string name) =>
                request.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
        }

        internal sealed record RecordedRequest(
            HttpMethod Method,
            string Url,
            string? MerchantId,
            string? MerchantKey,
            string Content);
    }
}
