using JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution;
using JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models;
using System.Net;
using System.Text;

namespace JotaSystem.Sdk.Providers.Tests.Providers
{
    public class EvolutionApiProviderTests
    {
        [Fact]
        public async Task CreateInstanceAsync_Should_Send_ApiKey_Settings_And_Webhook()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                {
                    "instance": { "instanceName": "jotahub-1", "instanceId": "abc", "status": "connecting" },
                    "hash": "instance-token",
                    "qrcode": { "pairingCode": null, "code": "2@xyz", "base64": "data:image/png;base64,AAA", "count": 1 }
                }
                """, HttpStatusCode.Created));
            var provider = CreateProvider(handler);

            var result = await provider.CreateInstanceAsync(new EvolutionCreateInstanceRequest
            {
                InstanceName = "jotahub-1",
                Settings = new EvolutionInstanceSettings { RejectCall = true, MsgCall = "Nao atendemos ligacoes." },
                Webhook = new EvolutionWebhookConfig { Url = "https://api.jotahub.com.br/webhooks/whatsapp/evolution/1" }
            }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal("instance-token", result.Data!.Token);
            Assert.Equal("abc", result.Data.InstanceId);
            Assert.Equal("data:image/png;base64,AAA", result.Data.QrCode!.Base64);

            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://evolution.test/instance/create", request.Url);
            Assert.Equal("global-key", request.ApiKey);
            Assert.Contains("\"instanceName\":\"jotahub-1\"", request.Content);
            Assert.Contains("\"integration\":\"WHATSAPP-BAILEYS\"", request.Content);
            Assert.Contains("\"rejectCall\":true", request.Content);
            Assert.Contains("\"msgCall\":\"Nao atendemos liga", request.Content);
            Assert.Contains("\"url\":\"https://api.jotahub.com.br/webhooks/whatsapp/evolution/1\"", request.Content);
            Assert.Contains("\"CONNECTION_UPDATE\"", request.Content);
            Assert.DoesNotContain("\"token\"", request.Content);
        }

        [Fact]
        public async Task CreateInstanceAsync_Should_Read_Legacy_Hash_Object()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                { "instance": { "instanceName": "x" }, "hash": { "apikey": "legacy-token" } }
                """, HttpStatusCode.Created));
            var provider = CreateProvider(handler);

            var result = await provider.CreateInstanceAsync(new EvolutionCreateInstanceRequest { InstanceName = "x" },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("legacy-token", result.Data!.Token);
        }

        [Fact]
        public async Task CreateInstanceAsync_Should_Describe_Error_Message_List()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                { "status": 403, "error": "Forbidden", "response": { "message": ["This name \"x\" is already in use."] } }
                """, HttpStatusCode.Forbidden));
            var provider = CreateProvider(handler);

            var result = await provider.CreateInstanceAsync(new EvolutionCreateInstanceRequest { InstanceName = "x" },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Equal("A Evolution API respondeu com o status 403: This name \"x\" is already in use.", result.ErrorMessage);
        }

        [Fact]
        public async Task Calls_Should_Fail_Without_Credentials()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = new EvolutionApiProvider(new TestHttpClientFactory(new HttpClient(handler)), new EvolutionApiOptions());

            var result = await provider.GetConnectionStateAsync("x", cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Calls_Should_Prefer_Credentials_Informed_On_Call()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""{"instance":{"instanceName":"x","state":"open"}}"""));
            var provider = CreateProvider(handler);

            var result = await provider.GetConnectionStateAsync("x",
                new EvolutionApiCredentials { BaseUrl = "https://outro.test/", ApiKey = "tenant-key" },
                TestContext.Current.CancellationToken);

            Assert.Equal(EvolutionConnectionStateEnum.Open, result.Data!.State);
            Assert.Equal("https://outro.test/instance/connectionState/x", handler.Requests[0].Url);
            Assert.Equal("tenant-key", handler.Requests[0].ApiKey);
        }

        [Fact]
        public async Task ConnectInstanceAsync_Should_Return_QrCode_And_Pairing_Code()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                { "pairingCode": "WZYEH1YY", "code": "2@abc", "base64": "data:image/png;base64,QR", "count": 2 }
                """));
            var provider = CreateProvider(handler);

            var result = await provider.ConnectInstanceAsync("x", "(11) 99999-8888",
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal(EvolutionConnectionStateEnum.Connecting, result.Data!.State);
            Assert.Equal("WZYEH1YY", result.Data.QrCode!.PairingCode);
            Assert.Equal(2, result.Data.QrCode.Count);
            Assert.Equal("https://evolution.test/instance/connect/x?number=5511999998888", handler.Requests[0].Url);
        }

        [Fact]
        public async Task ConnectInstanceAsync_Should_Report_Open_Session_Without_QrCode()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""{"instance":{"instanceName":"x","state":"open"}}"""));
            var provider = CreateProvider(handler);

            var result = await provider.ConnectInstanceAsync("x", cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Data!.IsConnected);
            Assert.Null(result.Data.QrCode);
        }

        [Fact]
        public async Task FetchInstancesAsync_Should_Read_V2_Format()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                [{
                    "id": "id-1", "name": "x", "connectionStatus": "open",
                    "ownerJid": "5511988887777@s.whatsapp.net", "profileName": "Clinica",
                    "profilePicUrl": "https://pps.whatsapp.net/p.jpg", "token": "t"
                }]
                """));
            var provider = CreateProvider(handler);

            var result = await provider.FetchInstancesAsync("x", cancellationToken: TestContext.Current.CancellationToken);

            var instance = Assert.Single(result.Data!);
            Assert.Equal("x", instance.InstanceName);
            Assert.Equal(EvolutionConnectionStateEnum.Open, instance.State);
            Assert.Equal("5511988887777", instance.PhoneNumber);
            Assert.Equal("Clinica", instance.ProfileName);
            Assert.Equal("https://evolution.test/instance/fetchInstances?instanceName=x", handler.Requests[0].Url);
        }

        [Fact]
        public async Task FetchInstancesAsync_Should_Read_V1_Format()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                [{ "instance": { "instanceName": "x", "owner": "5511988887777@s.whatsapp.net", "status": "close" } }]
                """));
            var provider = CreateProvider(handler);

            var result = await provider.FetchInstancesAsync(cancellationToken: TestContext.Current.CancellationToken);

            var instance = Assert.Single(result.Data!);
            Assert.Equal(EvolutionConnectionStateEnum.Close, instance.State);
            Assert.Equal("5511988887777", instance.PhoneNumber);
        }

        [Fact]
        public async Task FetchInstancesAsync_Should_Return_Empty_When_Instance_Does_Not_Exist()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                {"status":404,"error":"Not Found","response":{"message":["The \"x\" instance does not exist"]}}
                """, HttpStatusCode.NotFound));
            var provider = CreateProvider(handler);

            var result = await provider.FetchInstancesAsync("x", cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public async Task SendTextAsync_Should_Normalize_Number_And_Return_Message_Id()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                { "key": { "remoteJid": "5511999998888@s.whatsapp.net", "fromMe": true, "id": "BAE5F5A632EAE722" },
                  "status": "PENDING", "messageTimestamp": "1717689097" }
                """, HttpStatusCode.Created));
            var provider = CreateProvider(handler);

            var result = await provider.SendTextAsync("x",
                new EvolutionSendTextRequest { Number = "+55 (11) 99999-8888", Text = "Ola" },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal("BAE5F5A632EAE722", result.Data!.MessageId);
            Assert.Equal(1717689097, result.Data.MessageTimestamp);
            Assert.Equal("https://evolution.test/message/sendText/x", handler.Requests[0].Url);
            Assert.Contains("\"number\":\"5511999998888\"", handler.Requests[0].Content);
            Assert.Contains("\"text\":\"Ola\"", handler.Requests[0].Content);
            Assert.DoesNotContain("delay", handler.Requests[0].Content);
        }

        [Fact]
        public async Task SendTextAsync_Should_Reject_Invalid_Number()
        {
            var handler = new RecordingHttpMessageHandler();
            var provider = CreateProvider(handler);

            var result = await provider.SendTextAsync("x", new EvolutionSendTextRequest { Number = "1234", Text = "Ola" },
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task SendMediaAsync_Should_Strip_Data_Uri_Prefix()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""{"key":{"id":"m1"}}""", HttpStatusCode.Created));
            var provider = CreateProvider(handler);

            await provider.SendMediaAsync("x", new EvolutionSendMediaRequest
            {
                Number = "11999998888",
                MediaType = EvolutionMediaTypes.Document,
                MimeType = "application/pdf",
                Media = "data:application/pdf;base64,JVBERi0=",
                FileName = "proposta.pdf"
            }, cancellationToken: TestContext.Current.CancellationToken);

            var content = handler.Requests[0].Content;
            Assert.Contains("\"mediatype\":\"document\"", content);
            Assert.Contains("\"media\":\"JVBERi0=\"", content);
            Assert.Contains("\"fileName\":\"proposta.pdf\"", content);
        }

        [Fact]
        public async Task RestartInstanceAsync_Should_Fallback_To_Put_On_Legacy_Servers()
        {
            var handler = new RecordingHttpMessageHandler(
                CreateResponse("""{"statusCode":404,"message":"Cannot POST /instance/restart/x"}""", HttpStatusCode.NotFound),
                CreateResponse("""{"instance":{"instanceName":"x","state":"open"}}"""));
            var provider = CreateProvider(handler);

            var result = await provider.RestartInstanceAsync("x", cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        }

        [Fact]
        public async Task SetWebhookAsync_Should_Wrap_Config_In_Webhook_Node()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""{"id":"w1"}""", HttpStatusCode.Created));
            var provider = CreateProvider(handler);

            var result = await provider.SetWebhookAsync("x", new EvolutionWebhookConfig
            {
                Url = "https://api.test/hook",
                Headers = new Dictionary<string, string> { ["x-token"] = "s" },
                Events = ["connection.update"]
            }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(result.Success);
            Assert.Equal("https://evolution.test/webhook/set/x", handler.Requests[0].Url);
            Assert.Contains("\"webhook\":{\"enabled\":true,\"url\":\"https://api.test/hook\"", handler.Requests[0].Content);
            Assert.Contains("\"events\":[\"CONNECTION_UPDATE\"]", handler.Requests[0].Content);
            Assert.Contains("\"x-token\":\"s\"", handler.Requests[0].Content);
        }

        [Fact]
        public async Task CheckWhatsAppNumbersAsync_Should_Return_Existence()
        {
            var handler = new RecordingHttpMessageHandler(CreateResponse("""
                [{"exists":true,"jid":"5511999998888@s.whatsapp.net","number":"5511999998888"}]
                """));
            var provider = CreateProvider(handler);

            var result = await provider.CheckWhatsAppNumbersAsync("x", ["11999998888"],
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(Assert.Single(result.Data!).Exists);
            Assert.Contains("\"numbers\":[\"5511999998888\"]", handler.Requests[0].Content);
        }

        [Fact]
        public async Task Should_Report_Timeout()
        {
            var provider = CreateProvider(new ThrowingHttpMessageHandler(new TaskCanceledException()));

            var result = await provider.GetServerInfoAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.False(result.Success);
            Assert.Contains("Tempo limite", result.ErrorMessage);
        }

        [Fact]
        public void ParseWebhook_Should_Read_Connection_Update()
        {
            var result = EvolutionWebhookParser.Parse("""
                {
                    "event": "connection.update", "instance": "x",
                    "data": { "instance": "x", "state": "open", "statusReason": 200,
                              "wuid": "5511988887777@s.whatsapp.net", "profileName": "Clinica" },
                    "date_time": "2026-10-16T10:00:00.000Z", "apikey": "instance-token"
                }
                """);

            Assert.True(result.Success);
            Assert.Equal(EvolutionWebhookEvents.ConnectionUpdate, result.Data!.Event);
            Assert.Equal("x", result.Data.InstanceName);
            Assert.Equal("instance-token", result.Data.ApiKey);
            Assert.Equal(EvolutionConnectionStateEnum.Open, result.Data.State);
            Assert.Equal("5511988887777", result.Data.PhoneNumber);
            Assert.Equal(200, result.Data.StatusReason);
            Assert.NotNull(result.Data.OccurredAt);
        }

        [Fact]
        public void ParseWebhook_Should_Read_QrCode_Update()
        {
            var result = EvolutionWebhookParser.Parse("""
                { "event": "qrcode.updated", "instance": "x",
                  "data": { "qrcode": { "instance": "x", "code": "2@a", "base64": "data:image/png;base64,QR" } } }
                """);

            Assert.Equal(EvolutionWebhookEvents.QrCodeUpdated, result.Data!.Event);
            Assert.Equal("data:image/png;base64,QR", result.Data.QrCode!.Base64);
        }

        [Fact]
        public void ParseWebhook_Should_Read_Incoming_Text_Message()
        {
            var result = EvolutionWebhookParser.Parse("""
                { "event": "messages.upsert", "instance": "x",
                  "data": { "key": { "remoteJid": "5511977776666@s.whatsapp.net", "fromMe": false, "id": "M1" },
                            "pushName": "Maria", "message": { "conversation": "SIM" },
                            "messageType": "conversation", "messageTimestamp": 1717689097 } }
                """);

            var message = result.Data!.Message!;
            Assert.Equal("M1", message.MessageId);
            Assert.Equal("5511977776666", message.PhoneNumber);
            Assert.False(message.FromMe);
            Assert.False(message.IsGroup);
            Assert.Equal("SIM", message.Text);
            Assert.Equal("Maria", message.PushName);
        }

        [Fact]
        public void ParseWebhook_Should_Read_Message_Status_Update()
        {
            var result = EvolutionWebhookParser.Parse("""
                { "event": "MESSAGES_UPDATE", "instance": "x",
                  "data": { "keyId": "M1", "remoteJid": "5511977776666@s.whatsapp.net", "fromMe": true, "status": "READ" } }
                """);

            Assert.Equal(EvolutionWebhookEvents.MessagesUpdate, result.Data!.Event);
            Assert.Equal("M1", result.Data.Message!.MessageId);
            Assert.Equal("READ", result.Data.Message.Status);
            Assert.True(result.Data.Message.FromMe);
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("""{"instance":"x"}""")]
        [InlineData("")]
        public void ParseWebhook_Should_Fail_For_Invalid_Payload(string payload)
        {
            Assert.False(EvolutionWebhookParser.Parse(payload).Success);
        }

        [Theory]
        [InlineData("(11) 99999-8888", "5511999998888")]
        [InlineData("1133334444", "551133334444")]
        [InlineData("+55 11 99999-8888", "5511999998888")]
        [InlineData("011999998888", "5511999998888")]
        [InlineData("120363000000000000@g.us", "120363000000000000@g.us")]
        [InlineData("9999", null)]
        [InlineData(null, null)]
        public void PhoneNumber_Should_Normalize(string? value, string? expected)
        {
            Assert.Equal(expected, EvolutionPhoneNumber.Normalize(value));
        }

        private static EvolutionApiProvider CreateProvider(HttpMessageHandler handler) =>
            new(new TestHttpClientFactory(new HttpClient(handler)), new EvolutionApiOptions
            {
                DefaultCredentials = new EvolutionApiCredentials { BaseUrl = "https://evolution.test/", ApiKey = "global-key" }
            });

        private static HttpResponseMessage CreateResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            new(statusCode) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

        private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => client;
        }

        private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromException<HttpResponseMessage>(exception);
        }

        private sealed class RecordingHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new(responses);

            public List<RecordedRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri!.ToString(),
                    request.Headers.TryGetValues("apikey", out var apiKey) ? apiKey.Single() : null,
                    request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));

                return _responses.Count > 0 ? _responses.Dequeue() : CreateResponse("{}");
            }
        }

        private sealed record RecordedRequest(HttpMethod Method, string Url, string? ApiKey, string Content);
    }
}
