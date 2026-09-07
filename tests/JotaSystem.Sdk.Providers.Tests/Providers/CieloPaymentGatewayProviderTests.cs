using JotaSystem.Sdk.Core.CrossCutting.Providers.Enum;
using JotaSystem.Sdk.Core.CrossCutting.Providers.Models;
using JotaSystem.Sdk.Providers.Payments;
using JotaSystem.Sdk.Providers.Payments.Cielo;
using System.Net;
using static JotaSystem.Sdk.Providers.Tests.Providers.CieloProviderTests;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class CieloPaymentGatewayProviderTests
    {
        [Fact]
        public async Task CreateAsync_Should_Authorize_Credit_Card_With_Data_From_Metadata()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "6f8d1753-86bb-4dc0-9ebb-09a29093e1fb",
                        "Type": "CreditCard",
                        "Amount": 15700,
                        "Status": 2,
                        "Tid": "1124060407175",
                        "AuthorizationCode": "663864",
                        "ReturnCode": "6",
                        "ReturnMessage": "Operation Successful",
                        "Currency": "BRL"
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(
                    CieloMethodCodes.CreditCard,
                    card: new PaymentCard(
                        Number: "4091 6886 2533 7641",
                        Holder: "Aline de Souza",
                        ExpirationDate: "12/2035",
                        SecurityCode: "333",
                        Brand: "Visa")),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Paid, result.Status);
            Assert.Equal("6f8d1753-86bb-4dc0-9ebb-09a29093e1fb", result.TransactionId);
            Assert.Equal(157.00m, result.Amount);
            Assert.Equal("1124060407175", result.Metadata!["tid"]);

            var content = handler.Requests[0].Content;
            Assert.Contains("\"MerchantOrderId\":\"PED00012\"", content);
            Assert.Contains("\"CardNumber\":\"4091688625337641\"", content);
            Assert.Contains("\"Installments\":3", content);
            Assert.Contains("\"Capture\":true", content);
            Assert.Contains("\"Identity\":\"12345678909\"", content);
            Assert.Contains("\"IdentityType\":\"CPF\"", content);
        }

        [Fact]
        public async Task CreateAsync_Should_Schedule_Recurrence_For_Recurrent_Credit_Card()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "c5142036-956e-49ce-a0ad-680522b1bac4",
                        "Type": "CreditCard",
                        "Amount": 15700,
                        "Status": 1,
                        "ReturnMessage": "Operation Successful",
                        "RecurrentPayment": {
                            "RecurrentPaymentId": "ed535ad9-eb4f-4fef-9a77-a2c9620702ce",
                            "NextRecurrency": "2026-10-04",
                            "Interval": 1
                        }
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(
                    CieloMethodCodes.RecurrentCreditCard,
                    card: new PaymentCard(Token: "card-token"),
                    recurrence: new PaymentRecurrence(
                        PaymentRecurrenceIntervalEnum.Quarterly,
                        EndDate: new DateOnly(2030, 12, 31))),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Authorized, result.Status);
            Assert.Equal("ed535ad9-eb4f-4fef-9a77-a2c9620702ce", result.Metadata!["recurrent_payment_id"]);

            var content = handler.Requests[0].Content;
            Assert.Contains("\"CardToken\":\"card-token\"", content);
            Assert.Contains("\"RecurrentPayment\":{", content);
            Assert.Contains("\"AuthorizeNow\":true", content);
            Assert.Contains("\"EndDate\":\"2030-12-31\"", content);
            Assert.Contains("\"Interval\":\"Quarterly\"", content);
            Assert.Contains("\"Installments\":1", content);
        }

        [Fact]
        public async Task CreateAsync_Should_Authorize_With_The_Single_Use_Token_Alone()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "payment-id",
                        "Type": "CreditCard",
                        "Amount": 15700,
                        "Status": 2,
                        "ReturnMessage": "Operation Successful"
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(
                    CieloMethodCodes.CreditCard,
                    card: new PaymentCard(SingleUseToken: "sop-payment-token", Brand: "Visa")),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Paid, result.Status);

            var content = handler.Requests[0].Content;
            Assert.Contains("\"PaymentToken\":\"sop-payment-token\"", content);
            Assert.Contains("\"Brand\":\"Visa\"", content);
            Assert.DoesNotContain("CardNumber", content);
            Assert.DoesNotContain("SecurityCode", content);
        }

        [Fact]
        public async Task CreateCheckoutSessionAsync_Should_Open_A_Silent_Order_Post_Session()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"access_token":"oauth-token","expires_in":599}"""),
                CreateResponse("""
                {
                    "AccessToken": "sop-access-token",
                    "Issued": "2026-09-04T08:50:04",
                    "ExpiresIn": "2026-09-04T09:10:04"
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler, options =>
            {
                options.DefaultCredentials!.ClientId = "client-id";
                options.DefaultCredentials.ClientSecret = "client-secret";
            });

            var session = await provider.CreateCheckoutSessionAsync(
                new PaymentCheckoutSessionRequest("cielo"),
                TestContext.Current.CancellationToken);

            Assert.True(session.IsSuccess);
            Assert.Equal("sop-access-token", session.AccessToken);
            Assert.Equal("sandbox", session.Environment);
            Assert.EndsWith("silentorderpost-1.0.min.js", session.ScriptUrl);
            Assert.NotNull(session.ExpiresAt);
        }

        [Fact]
        public async Task CreateCheckoutSessionAsync_Should_Fail_Without_Oauth_Credentials()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateGateway(handler);

            var session = await provider.CreateCheckoutSessionAsync(
                new PaymentCheckoutSessionRequest("cielo"),
                TestContext.Current.CancellationToken);

            Assert.False(session.IsSuccess);
            Assert.Null(session.AccessToken);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateAsync_Should_Return_QrCode_For_Pix()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "1997be4d-694a-472e-98f0-e7f4b4c8f1e7",
                        "Type": "Pix",
                        "QrcodeBase64Image": "cXJjb2Rl",
                        "QrCodeString": "00020101021226880014br.gov.bcb.pix",
                        "Amount": 15700,
                        "Status": 12,
                        "ReturnMessage": "Pix gerado com sucesso"
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(CieloMethodCodes.Pix),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Pending, result.Status);
            Assert.Equal("00020101021226880014br.gov.bcb.pix", result.QrCode);
            Assert.Equal("cXJjb2Rl", result.Metadata!["qr_code_base64"]);
            Assert.Contains("\"Type\":\"Pix\"", handler.Requests[0].Content);
        }

        [Fact]
        public async Task CreateAsync_Should_Return_Boleto_Data_And_Keep_It_Pending()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "259629d4-1e45-4017-9064-ea09a4ccd4d6",
                        "Type": "Boleto",
                        "Url": "https://transactionsandbox.pagador.com.br/post/pagador/reenvia.asp/259629d4",
                        "BoletoNumber": "512-2",
                        "BarCodeNumber": "00091496400000157009999250000000051299999990",
                        "DigitableLine": "00099.99921 50000.000054 12999.999902 1 49640000015700",
                        "ExpirationDate": "2026-09-20",
                        "Amount": 15700,
                        "Status": 1
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(
                    CieloMethodCodes.Boleto,
                    address: new PaymentAddress(
                        Street: "Alameda Xingu",
                        Number: "512",
                        ZipCode: "12345-987",
                        City: "Sao Paulo",
                        State: "SP")),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Pending, result.Status);
            Assert.Equal("00099.99921 50000.000054 12999.999902 1 49640000015700", result.Barcode);
            Assert.NotNull(result.PaymentUrl);
            Assert.Equal(new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero), result.ExpiresAt);

            var content = handler.Requests[0].Content;
            Assert.Contains("\"Type\":\"Boleto\"", content);
            Assert.Contains("\"Provider\":\"Bradesco2\"", content);
            Assert.Contains("\"ZipCode\":\"12345987\"", content);
            Assert.Contains("\"ExpirationDate\":\"2026-09-30\"", content);
        }

        [Fact]
        public async Task CreateAsync_Should_Fail_When_Method_Is_Not_Supported()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest("debito_online"),
                TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Failed, result.Status);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateAsync_Should_Report_Denied_Payment_As_Failure()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "payment-id",
                        "Type": "CreditCard",
                        "Amount": 15700,
                        "Status": 3,
                        "ReturnCode": "05",
                        "ReturnMessage": "Not Authorized"
                    }
                }
                """, HttpStatusCode.Created));
            var provider = CreateGateway(handler);

            var result = await provider.CreateAsync(
                CreateRequest(CieloMethodCodes.CreditCard, card: new PaymentCard(Token: "card-token")),
                TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Failed, result.Status);
            Assert.Equal("Not Authorized", result.Message);
        }

        [Fact]
        public async Task CreateAsync_Should_Use_Credentials_From_The_Integration_Context()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"Payment":{"PaymentId":"payment-id","Type":"Pix","Status":12}}""",
                    HttpStatusCode.Created));
            var provider = new CieloPaymentGatewayProviderFactory(handler).CreateWithoutDefaultCredentials();

            var request = CreateRequest(CieloMethodCodes.Pix) with
            {
                Context = new PaymentProviderContext(
                    "production",
                    """{ "merchantId": "loja-merchant-id" }""",
                    "loja-merchant-key")
            };

            var result = await provider.CreateAsync(request, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal("loja-merchant-id", handler.Requests[0].MerchantId);
            Assert.Equal("loja-merchant-key", handler.Requests[0].MerchantKey);
            Assert.StartsWith("https://api.cieloecommerce.cielo.com.br/", handler.Requests[0].Url);
        }

        [Fact]
        public async Task GetAsync_Should_Report_Denied_Status_Without_Failing_The_Query()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "payment-id",
                        "Type": "CreditCard",
                        "Amount": 15700,
                        "Status": 3,
                        "ReturnMessage": "Not Authorized"
                    }
                }
                """));
            var provider = CreateGateway(handler);

            var result = await provider.GetAsync(
                new PaymentProviderQuery("cielo", "payment-id"),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Failed, result.Status);
            Assert.Equal("Not Authorized", result.Message);
        }

        [Fact]
        public async Task CancelAsync_Should_Void_The_Transaction()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"Status":10,"ReturnCode":"9","ReturnMessage":"Operation Successful"}"""));
            var provider = CreateGateway(handler);

            var result = await provider.CancelAsync(
                new PaymentProviderOperation("cielo", "payment-id"),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Cancelled, result.Status);
            Assert.EndsWith("/1/sales/payment-id/void", handler.Requests[0].Url);
        }

        [Fact]
        public async Task RefundAsync_Should_Send_The_Amount_In_Cents()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"Status":11,"ReturnMessage":"Operation Successful"}"""));
            var provider = CreateGateway(handler);

            var result = await provider.RefundAsync(
                new PaymentProviderRefund("cielo", "payment-id", 57.35m),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(PaymentProviderStatusEnum.Refunded, result.Status);
            Assert.EndsWith("/1/sales/payment-id/void?amount=5735", handler.Requests[0].Url);
        }

        [Fact]
        public async Task ParseWebhookAsync_Should_Confirm_The_Notification_On_Cielo()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""
                {
                    "MerchantOrderId": "PED00012",
                    "Payment": {
                        "PaymentId": "6f8d1753-86bb-4dc0-9ebb-09a29093e1fb",
                        "Type": "Pix",
                        "Amount": 15700,
                        "Currency": "BRL",
                        "Status": 2,
                        "ReceivedDate": "2026-09-04 10:15:00"
                    }
                }
                """));
            var provider = CreateGateway(handler);

            var webhookEvent = await provider.ParseWebhookAsync(
                new PaymentWebhookRequest(
                    "cielo",
                    """{"PaymentId":"6f8d1753-86bb-4dc0-9ebb-09a29093e1fb","ChangeType":1}""",
                    new Dictionary<string, string>()),
                TestContext.Current.CancellationToken);

            Assert.Equal("6f8d1753-86bb-4dc0-9ebb-09a29093e1fb", webhookEvent.TransactionId);
            Assert.Equal(PaymentProviderStatusEnum.Paid, webhookEvent.Status);
            Assert.Equal("payment.paid", webhookEvent.EventType);
            Assert.Equal(157.00m, webhookEvent.Amount);
            Assert.Equal(new DateTimeOffset(2026, 9, 4, 10, 15, 0, TimeSpan.Zero), webhookEvent.OccurredAt);
            Assert.StartsWith("https://apiquerysandbox.cieloecommerce.cielo.com.br/", handler.Requests[0].Url);
        }

        [Fact]
        public async Task ParseWebhookAsync_Should_Reject_Notification_Without_The_Configured_Secret()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateGateway(handler, options => options.WebhookHeaderName = "x-cielo-token");

            var request = new PaymentWebhookRequest(
                "cielo",
                """{"PaymentId":"payment-id","ChangeType":1}""",
                new Dictionary<string, string> { ["x-cielo-token"] = "outro-valor" },
                "segredo");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.ParseWebhookAsync(request, TestContext.Current.CancellationToken));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ParseWebhookAsync_Should_Throw_When_Payload_Has_No_PaymentId()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateGateway(handler);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                provider.ParseWebhookAsync(
                    new PaymentWebhookRequest("cielo", """{"ChangeType":1}""", new Dictionary<string, string>()),
                    TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ProviderKey_Should_Be_Cielo()
        {
            var provider = CreateGateway(new RecordingHttpMessageHandler());

            Assert.Equal("cielo", provider.ProviderKey);
        }

        private static IPaymentGatewayProvider CreateGateway(
            HttpMessageHandler handler,
            Action<CieloOptions>? configure = null)
        {
            var options = new CieloOptions { DefaultCredentials = CreateCredentials() };
            configure?.Invoke(options);

            var cieloProvider = new CieloProvider(new TestHttpClientFactory(new HttpClient(handler)), options, new CieloAuthTokenCache());
            return new CieloPaymentGatewayProvider(cieloProvider, options);
        }

        private static PaymentProviderRequest CreateRequest(
            string methodCode,
            Dictionary<string, string>? metadata = null,
            PaymentCard? card = null,
            PaymentRecurrence? recurrence = null,
            PaymentAddress? address = null) =>
            new(
                "cielo",
                "idempotency-key",
                "PED-00012",
                157.00m,
                "BRL",
                methodCode,
                3,
                new PaymentCustomer("Aline de Souza", "123.456.789-09", "aline@email.com", Address: address),
                ExpiresAt: new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero),
                Metadata: metadata,
                Card: card,
                Recurrence: recurrence);

        private sealed class CieloPaymentGatewayProviderFactory(HttpMessageHandler handler)
        {
            public IPaymentGatewayProvider CreateWithoutDefaultCredentials()
            {
                var options = new CieloOptions();
                var cieloProvider = new CieloProvider(new TestHttpClientFactory(new HttpClient(handler)), options, new CieloAuthTokenCache());
                return new CieloPaymentGatewayProvider(cieloProvider, options);
            }
        }
    }
}
