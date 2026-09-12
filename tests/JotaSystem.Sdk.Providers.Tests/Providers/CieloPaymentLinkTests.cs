using JotaSystem.Sdk.Core.CrossCutting.Providers.Enum;
using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;
using JotaSystem.Sdk.Providers.Payments;
using JotaSystem.Sdk.Providers.Payments.Cielo;
using JotaSystem.Sdk.Providers.Payments.Cielo.Link;
using JotaSystem.Sdk.Providers.Payments.Cielo.Link.Models;
using System.Net;
using static JotaSystem.Sdk.Providers.Tests.Providers.CieloProviderTests;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class CieloPaymentLinkTests
    {
        private const string LinkId = "0d4e2f4c-1d0e-4a4a-9a54-0f3f2b0d4b11";

        [Fact]
        public async Task CreateAsync_Should_Create_The_Payment_Link_And_Return_The_Short_Url()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse($$"""
                {
                    "id": "{{LinkId}}",
                    "shortUrl": "https://cielolink.com.br/abc123",
                    "orderNumber": "PED00012",
                    "type": "Payment",
                    "price": 15700,
                    "expirationDate": "2026-09-30"
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(CieloMethodCodes.PaymentLink),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Pending, result.Status);
            Assert.Equal(LinkId, result.TransactionId);
            Assert.Equal("https://cielolink.com.br/abc123", result.PaymentUrl!.ToString());
            Assert.Equal(157.00m, result.Amount);
            Assert.Equal(new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero), result.ExpiresAt);

            var token = handler.Requests[0];
            Assert.EndsWith("/api/public/v2/token", token.Url);

            var creation = handler.Requests[1];
            Assert.EndsWith("/api/public/v1/products/", creation.Url);
            Assert.Contains("\"orderNumber\":\"PED00012\"", creation.Content);
            Assert.Contains("\"name\":\"Pedido PED-00012\"", creation.Content);
            Assert.Contains("\"price\":15700", creation.Content);
            Assert.Contains("\"maxNumberOfInstallments\":3", creation.Content);
            Assert.Contains("\"type\":\"WithoutShipping\"", creation.Content);
        }

        [Fact]
        public async Task CreateAsync_Should_Turn_The_Link_Into_A_Subscription_When_There_Is_Recurrence()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse($$"""{"id":"{{LinkId}}","shortUrl":"https://cielolink.com.br/abc123"}""",
                    HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            await provider.CreateAsync(
                CreateRequest(
                    CieloMethodCodes.PaymentLink,
                    recurrence: new PaymentRecurrence(
                        PaymentRecurrenceIntervalEnum.Quarterly,
                        EndDate: new DateOnly(2027, 3, 31))),
                TestContext.Current.CancellationToken);

            var creation = handler.Requests[1].Content;
            Assert.Contains("\"type\":\"Recurrent\"", creation);
            Assert.Contains("\"interval\":\"Quarterly\"", creation);
            Assert.Contains("\"endDate\":\"2027-03-31\"", creation);
            Assert.DoesNotContain("maxNumberOfInstallments", creation);
        }

        [Fact]
        public async Task CreateAsync_Should_Fail_Without_The_Payment_Link_Credentials()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateGateway(handler, options => options.DefaultLinkCredentials = null);

            var result = await provider.CreateAsync(
                CreateRequest(CieloMethodCodes.PaymentLink),
                TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Contains("Link de Pagamento", result.Message);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GetAsync_Should_Read_The_Status_Of_The_Order_Generated_By_The_Link()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse($$"""
                {
                    "productId": "{{LinkId}}",
                    "orders": [
                        {
                            "orderNumber": "1e7e0b9f8c",
                            "createdDate": "2026-09-12 10:00:00",
                            "payment": { "price": 15700, "status": "Paid", "tid": "1124060407175" }
                        }
                    ]
                }
                """));
            var provider = CreateGateway(handler);

            var result = await provider.GetAsync(
                new PaymentProviderQuery("cielo", LinkId, MethodCode: CieloMethodCodes.PaymentLink),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Paid, result.Status);
            Assert.Equal(LinkId, result.TransactionId);
            Assert.Equal(157.00m, result.Amount);
            Assert.EndsWith($"/api/public/v1/products/{LinkId}/payments", handler.Requests[1].Url);
        }

        [Fact]
        public async Task GetAsync_Should_Keep_The_Charge_Pending_While_Nobody_Paid_The_Link()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse($$"""{"productId":"{{LinkId}}","orders":[]}"""),
                CreateResponse($$"""{"id":"{{LinkId}}","shortUrl":"https://cielolink.com.br/abc123"}"""));
            var provider = CreateGateway(handler);

            var result = await provider.GetAsync(
                new PaymentProviderQuery("cielo", LinkId, MethodCode: CieloMethodCodes.PaymentLink),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Pending, result.Status);
            Assert.Equal("https://cielolink.com.br/abc123", result.PaymentUrl!.ToString());
        }

        [Fact]
        public async Task CancelAsync_Should_Delete_The_Link_When_There_Is_No_Order()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse($$"""{"productId":"{{LinkId}}","orders":[]}"""),
                CreateResponse(string.Empty));
            var provider = CreateGateway(handler);

            var result = await provider.CancelAsync(
                new PaymentProviderOperation("cielo", LinkId, MethodCode: CieloMethodCodes.PaymentLink),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Cancelled, result.Status);
            Assert.Equal(HttpMethod.Delete, handler.Requests[2].Method);
            Assert.EndsWith($"/api/public/v1/products/{LinkId}", handler.Requests[2].Url);
        }

        [Fact]
        public async Task CancelAsync_Should_Void_The_Checkout_Order_When_The_Link_Was_Paid()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse($$"""
                {
                    "productId": "{{LinkId}}",
                    "orders": [
                        {
                            "checkoutCieloOrderNumber": "9f1c2d",
                            "payment": { "price": 15700, "status": "Paid" }
                        }
                    ]
                }
                """),
                CreateResponse("""{"success":true,"status":2,"returnCode":"6","returnMessage":"Operation Successful"}"""));
            var provider = CreateGateway(handler);

            var result = await provider.CancelAsync(
                new PaymentProviderOperation("cielo", LinkId, MethodCode: CieloMethodCodes.PaymentLink),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Cancelled, result.Status);
            Assert.Equal(HttpMethod.Put, handler.Requests[2].Method);
            Assert.EndsWith("/api/public/v2/orders/9f1c2d/void", handler.Requests[2].Url);
        }

        [Fact]
        public async Task ParseWebhookAsync_Should_Read_The_Form_Notification_And_Confirm_It_On_Cielo()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse("""
                {
                    "orderNumber": "PED00012",
                    "payment": { "price": 15700, "status": "Paid", "tid": "1124060407175" }
                }
                """));
            var provider = CreateGateway(handler);

            var payload = $"checkout_cielo_order_number=9f1c2d&order_number=PED00012&product_id={LinkId}" +
                          "&amount=15700&payment_status=1&payment_method_type=1" +
                          "&created_date=12-09-2026+10%3A15%3A00";

            var webhookEvent = await provider.ParseWebhookAsync(
                new PaymentWebhookRequest("cielo", payload, new Dictionary<string, string>()),
                TestContext.Current.CancellationToken);

            Assert.Equal(LinkId, webhookEvent.TransactionId);
            Assert.Equal(PaymentProviderStatusEnum.Paid, webhookEvent.Status);
            Assert.Equal("payment.paid", webhookEvent.EventType);
            Assert.Equal(157.00m, webhookEvent.Amount);
            Assert.Equal(new DateTimeOffset(2026, 9, 12, 10, 15, 0, TimeSpan.Zero), webhookEvent.OccurredAt);
            Assert.Equal("9f1c2d", webhookEvent.Metadata!["checkout_cielo_order_number"]);
            Assert.Equal("true", webhookEvent.Metadata["confirmed_by_query"]);
        }

        [Fact]
        public async Task ParseWebhookAsync_Should_Fall_Back_To_The_Notification_When_The_Query_Fails()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateTokenResponse(),
                CreateResponse("""{"Message":"Order not found"}""", HttpStatusCode.BadRequest));
            var provider = CreateGateway(handler);

            var payload = $"checkout_cielo_order_number=9f1c2d&product_id={LinkId}&amount=15700&payment_status=3";

            var webhookEvent = await provider.ParseWebhookAsync(
                new PaymentWebhookRequest("cielo", payload, new Dictionary<string, string>()),
                TestContext.Current.CancellationToken);

            Assert.Equal(PaymentProviderStatusEnum.Failed, webhookEvent.Status);
            Assert.Equal("false", webhookEvent.Metadata!["confirmed_by_query"]);
        }

        [Fact]
        public void TryParse_Should_Read_The_Notification_In_Json()
        {
            var notification = CieloLinkNotification.TryParse("""
            {
                "checkout_cielo_order_number": "9f1c2d",
                "product_id": "link-id",
                "amount": 15700,
                "payment_status": 2,
                "test_transaction": true
            }
            """);

            Assert.NotNull(notification);
            Assert.Equal("9f1c2d", notification.CheckoutCieloOrderNumber);
            Assert.Equal("link-id", notification.ProductId);
            Assert.Equal(15700, notification.Amount);
            Assert.Equal(CieloLinkStatusEnum.Paid, notification.PaymentStatus);
            Assert.True(notification.IsTest);
        }

        [Fact]
        public void TryParse_Should_Ignore_A_Payload_From_The_Ecommerce_Api()
        {
            var notification = CieloLinkNotification.TryParse("""{"PaymentId":"payment-id","ChangeType":1}""");

            Assert.Null(notification);
        }

        private static IPaymentGatewayProvider CreateGateway(
            HttpMessageHandler handler,
            Action<CieloOptions>? configure = null)
        {
            var options = new CieloOptions
            {
                DefaultCredentials = CreateCredentials(),
                DefaultLinkCredentials = new CieloLinkCredentials
                {
                    ClientId = "link-client-id",
                    ClientSecret = "link-client-secret"
                }
            };
            configure?.Invoke(options);

            var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
            var authTokenCache = new CieloAuthTokenCache();
            var cieloProvider = new CieloProvider(httpClientFactory, options, authTokenCache);
            var linkProvider = new CieloLinkProvider(httpClientFactory, options, authTokenCache);
            return new CieloPaymentGatewayProvider(cieloProvider, linkProvider, options);
        }

        private static HttpResponseMessage CreateTokenResponse() =>
            CreateResponse("""{"access_token":"link-access-token","token_type":"bearer","expires_in":1199}""");

        private static PaymentProviderRequest CreateRequest(
            string methodCode,
            PaymentRecurrence? recurrence = null) =>
            new(
                "cielo",
                "idempotency-key",
                "PED-00012",
                157.00m,
                "BRL",
                methodCode,
                3,
                new PaymentCustomer("Aline de Souza", "123.456.789-09", "aline@email.com"),
                ExpiresAt: new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero),
                Recurrence: recurrence);
    }
}
