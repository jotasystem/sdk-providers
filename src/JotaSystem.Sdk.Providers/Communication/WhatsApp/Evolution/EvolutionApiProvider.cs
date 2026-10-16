using JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    internal sealed class EvolutionApiProvider(
        IHttpClientFactory httpClientFactory,
        EvolutionApiOptions options) : IEvolutionApiProvider
    {
        private const string DataUriPrefix = "data:";

        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly EvolutionApiOptions _options = options;

        public async Task<ApiResponse<EvolutionServerInfo>> GetServerInfoAsync(
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<EvolutionServerInfo>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(HttpMethod.Get, string.Empty, selectedCredentials, body: null, cancellationToken);
            return Read(response, root => new EvolutionServerInfo
            {
                Status = EvolutionApiJson.ReadInt(root, "status"),
                Message = EvolutionApiJson.ReadString(root, "message"),
                Version = EvolutionApiJson.ReadString(root, "version"),
                ClientName = EvolutionApiJson.ReadString(root, "clientName"),
                ManagerUrl = EvolutionApiJson.ReadString(root, "manager")
            });
        }

        public async Task<ApiResponse<List<EvolutionInstanceInfo>>> FetchInstancesAsync(
            string? instanceName = null,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<List<EvolutionInstanceInfo>>.CreateFail(MissingCredentialsMessage);

            var path = string.IsNullOrWhiteSpace(instanceName)
                ? "instance/fetchInstances"
                : $"instance/fetchInstances?instanceName={Uri.EscapeDataString(instanceName.Trim())}";

            var response = await SendAsync(HttpMethod.Get, path, selectedCredentials, body: null, cancellationToken);

            // Instancia inexistente responde 404 em algumas versoes: para quem consulta, e uma lista vazia.
            if (!response.IsSuccess && response.StatusCode == HttpStatusCode.NotFound && !string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<List<EvolutionInstanceInfo>>.CreateSuccess([]);

            return Read(response, root =>
            {
                var items = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : [root];
                return items
                    .Select(ReadInstance)
                    .Where(x => !string.IsNullOrWhiteSpace(x.InstanceName))
                    .ToList();
            });
        }

        public async Task<ApiResponse<EvolutionCreateInstanceResponse>> CreateInstanceAsync(
            EvolutionCreateInstanceRequest request,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.InstanceName))
                return ApiResponse<EvolutionCreateInstanceResponse>.CreateFail("Informe o nome da instancia.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<EvolutionCreateInstanceResponse>.CreateFail(MissingCredentialsMessage);

            var body = new Dictionary<string, object?>
            {
                ["instanceName"] = request.InstanceName.Trim(),
                ["token"] = string.IsNullOrWhiteSpace(request.Token) ? null : request.Token.Trim(),
                ["qrcode"] = request.QrCode,
                ["number"] = NormalizeNumber(request.Number),
                ["integration"] = string.IsNullOrWhiteSpace(request.Integration) ? _options.Integration : request.Integration
            };

            if (request.Settings is { } settings)
            {
                body["rejectCall"] = settings.RejectCall;
                body["msgCall"] = settings.RejectCall ? settings.MsgCall : null;
                body["groupsIgnore"] = settings.GroupsIgnore;
                body["alwaysOnline"] = settings.AlwaysOnline;
                body["readMessages"] = settings.ReadMessages;
                body["readStatus"] = settings.ReadStatus;
                body["syncFullHistory"] = settings.SyncFullHistory;
            }

            if (request.Webhook is { } webhook && !string.IsNullOrWhiteSpace(webhook.Url))
                body["webhook"] = BuildWebhook(webhook);

            var response = await SendAsync(HttpMethod.Post, "instance/create", selectedCredentials,
                RemoveNulls(body), cancellationToken);

            return Read(response, root => new EvolutionCreateInstanceResponse
            {
                InstanceName = EvolutionApiJson.ReadString(root, "instance", "instanceName") ?? request.InstanceName.Trim(),
                InstanceId = EvolutionApiJson.ReadString(root, "instance", "instanceId"),
                Status = EvolutionApiJson.ReadString(root, "instance", "status"),
                // v2 devolve "hash" como texto; v1 devolvia { "apikey": "..." }.
                Token = EvolutionApiJson.ReadString(root, "hash")
                        ?? EvolutionApiJson.ReadString(root, "hash", "apikey"),
                QrCode = EvolutionApiJson.TryGet(root, out var qrCode, "qrcode") ? ReadQrCode(qrCode) : null,
                RawPayload = response.Content
            });
        }

        public async Task<ApiResponse<EvolutionConnectResponse>> ConnectInstanceAsync(
            string instanceName,
            string? phoneNumber = null,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<EvolutionConnectResponse>.CreateFail(MissingInstanceMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<EvolutionConnectResponse>.CreateFail(MissingCredentialsMessage);

            var path = $"instance/connect/{Escape(instanceName)}";
            var number = NormalizeNumber(phoneNumber);
            if (number is not null)
                path = $"{path}?number={Uri.EscapeDataString(number)}";

            var response = await SendAsync(HttpMethod.Get, path, selectedCredentials, body: null, cancellationToken);
            return Read(response, root =>
            {
                // Sessao ja aberta: o servidor devolve apenas { instance: { state: "open" } }.
                var state = EvolutionApiJson.ReadString(root, "instance", "state");
                var qrCode = ReadQrCode(root);

                return new EvolutionConnectResponse
                {
                    State = state is not null
                        ? EvolutionConnectionStates.Parse(state)
                        : qrCode.HasContent ? EvolutionConnectionStateEnum.Connecting : EvolutionConnectionStateEnum.Unknown,
                    QrCode = qrCode.HasContent ? qrCode : null,
                    RawPayload = response.Content
                };
            });
        }

        public async Task<ApiResponse<EvolutionConnectionState>> GetConnectionStateAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<EvolutionConnectionState>.CreateFail(MissingInstanceMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<EvolutionConnectionState>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(HttpMethod.Get, $"instance/connectionState/{Escape(instanceName)}",
                selectedCredentials, body: null, cancellationToken);

            return Read(response, root =>
            {
                var rawState = EvolutionApiJson.ReadString(root, "instance", "state")
                               ?? EvolutionApiJson.ReadString(root, "state");
                return new EvolutionConnectionState
                {
                    InstanceName = EvolutionApiJson.ReadString(root, "instance", "instanceName") ?? instanceName.Trim(),
                    State = EvolutionConnectionStates.Parse(rawState),
                    RawState = rawState
                };
            });
        }

        public async Task<ApiResponse<bool>> RestartInstanceAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<bool>.CreateFail(MissingInstanceMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            var path = $"instance/restart/{Escape(instanceName)}";

            // A v2 usa POST; a v1 usava PUT. Tentar os dois mantem a compatibilidade com servidores antigos.
            var response = await SendAsync(HttpMethod.Post, path, selectedCredentials, body: null, cancellationToken);
            if (!response.IsSuccess && response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed
                && !IsInstanceNotFound(response.Content))
                response = await SendAsync(HttpMethod.Put, path, selectedCredentials, body: null, cancellationToken);

            return ToBool(response);
        }

        public Task<ApiResponse<bool>> LogoutInstanceAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default) =>
            SendInstanceCommandAsync(HttpMethod.Delete, "instance/logout", instanceName, credentials, cancellationToken);

        public Task<ApiResponse<bool>> DeleteInstanceAsync(
            string instanceName,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default) =>
            SendInstanceCommandAsync(HttpMethod.Delete, "instance/delete", instanceName, credentials, cancellationToken);

        public async Task<ApiResponse<bool>> SetWebhookAsync(
            string instanceName,
            EvolutionWebhookConfig webhook,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<bool>.CreateFail(MissingInstanceMessage);

            if (webhook is null || (webhook.Enabled && !Uri.TryCreate(webhook.Url, UriKind.Absolute, out _)))
                return ApiResponse<bool>.CreateFail("Informe uma URL absoluta para o webhook.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(HttpMethod.Post, $"webhook/set/{Escape(instanceName)}", selectedCredentials,
                new Dictionary<string, object?> { ["webhook"] = BuildWebhook(webhook) }, cancellationToken);

            return ToBool(response);
        }

        public async Task<ApiResponse<bool>> SetSettingsAsync(
            string instanceName,
            EvolutionInstanceSettings settings,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<bool>.CreateFail(MissingInstanceMessage);

            if (settings is null)
                return ApiResponse<bool>.CreateFail("Informe as configuracoes da instancia.");

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            var body = new Dictionary<string, object?>
            {
                ["rejectCall"] = settings.RejectCall,
                ["msgCall"] = settings.RejectCall ? settings.MsgCall ?? string.Empty : string.Empty,
                ["groupsIgnore"] = settings.GroupsIgnore,
                ["alwaysOnline"] = settings.AlwaysOnline,
                ["readMessages"] = settings.ReadMessages,
                ["readStatus"] = settings.ReadStatus,
                ["syncFullHistory"] = settings.SyncFullHistory
            };

            var response = await SendAsync(HttpMethod.Post, $"settings/set/{Escape(instanceName)}", selectedCredentials,
                body, cancellationToken);

            return ToBool(response);
        }

        public async Task<ApiResponse<EvolutionMessageResponse>> SendTextAsync(
            string instanceName,
            EvolutionSendTextRequest request,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<EvolutionMessageResponse>.CreateFail(MissingInstanceMessage);

            if (request is null || string.IsNullOrWhiteSpace(request.Text))
                return ApiResponse<EvolutionMessageResponse>.CreateFail("Informe o texto da mensagem.");

            var number = NormalizeNumber(request.Number);
            if (number is null)
                return ApiResponse<EvolutionMessageResponse>.CreateFail(InvalidNumberMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<EvolutionMessageResponse>.CreateFail(MissingCredentialsMessage);

            var body = RemoveNulls(new Dictionary<string, object?>
            {
                ["number"] = number,
                ["text"] = request.Text,
                ["delay"] = request.Delay,
                ["linkPreview"] = request.LinkPreview
            });

            var response = await SendAsync(HttpMethod.Post, $"message/sendText/{Escape(instanceName)}",
                selectedCredentials, body, cancellationToken);

            return Read(response, root => ReadMessage(root, response.Content));
        }

        public async Task<ApiResponse<EvolutionMessageResponse>> SendMediaAsync(
            string instanceName,
            EvolutionSendMediaRequest request,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<EvolutionMessageResponse>.CreateFail(MissingInstanceMessage);

            if (request is null || string.IsNullOrWhiteSpace(request.Media))
                return ApiResponse<EvolutionMessageResponse>.CreateFail("Informe a URL ou o conteudo da midia.");

            if (string.IsNullOrWhiteSpace(request.MediaType))
                return ApiResponse<EvolutionMessageResponse>.CreateFail("Informe o tipo da midia.");

            var number = NormalizeNumber(request.Number);
            if (number is null)
                return ApiResponse<EvolutionMessageResponse>.CreateFail(InvalidNumberMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<EvolutionMessageResponse>.CreateFail(MissingCredentialsMessage);

            var body = RemoveNulls(new Dictionary<string, object?>
            {
                ["number"] = number,
                ["mediatype"] = request.MediaType.Trim().ToLowerInvariant(),
                ["mimetype"] = request.MimeType,
                ["caption"] = request.Caption,
                ["media"] = StripDataUri(request.Media.Trim()),
                ["fileName"] = request.FileName,
                ["delay"] = request.Delay
            });

            var response = await SendAsync(HttpMethod.Post, $"message/sendMedia/{Escape(instanceName)}",
                selectedCredentials, body, cancellationToken);

            return Read(response, root => ReadMessage(root, response.Content));
        }

        public async Task<ApiResponse<List<EvolutionNumberCheck>>> CheckWhatsAppNumbersAsync(
            string instanceName,
            IEnumerable<string> numbers,
            EvolutionApiCredentials? credentials = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<List<EvolutionNumberCheck>>.CreateFail(MissingInstanceMessage);

            var normalized = (numbers ?? [])
                .Select(NormalizeNumber)
                .Where(x => x is not null)
                .Distinct()
                .ToList();

            if (normalized.Count == 0)
                return ApiResponse<List<EvolutionNumberCheck>>.CreateFail(InvalidNumberMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<List<EvolutionNumberCheck>>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(HttpMethod.Post, $"chat/whatsappNumbers/{Escape(instanceName)}",
                selectedCredentials, new Dictionary<string, object?> { ["numbers"] = normalized }, cancellationToken);

            return Read(response, root => root.ValueKind != JsonValueKind.Array
                ? []
                : root.EnumerateArray()
                    .Select(item => new EvolutionNumberCheck
                    {
                        Number = EvolutionApiJson.ReadString(item, "number") ?? string.Empty,
                        Exists = EvolutionApiJson.ReadBool(item, "exists") ?? false,
                        Jid = EvolutionApiJson.ReadString(item, "jid")
                    })
                    .ToList());
        }

        public ApiResponse<EvolutionWebhookEvent> ParseWebhook(string payload) => EvolutionWebhookParser.Parse(payload);

        internal static EvolutionQrCode ReadQrCode(JsonElement element) => new()
        {
            Code = EvolutionApiJson.ReadString(element, "code"),
            Base64 = EvolutionApiJson.ReadString(element, "base64"),
            PairingCode = EvolutionApiJson.ReadString(element, "pairingCode"),
            Count = EvolutionApiJson.ReadInt(element, "count")
        };

        private static EvolutionInstanceInfo ReadInstance(JsonElement item)
        {
            // v1 encapsula os dados em { instance: { ... } }; v2 devolve o objeto direto.
            var data = EvolutionApiJson.TryGet(item, out var nested, "instance") && nested.ValueKind == JsonValueKind.Object
                ? nested
                : item;

            var rawState = EvolutionApiJson.ReadString(data, "connectionStatus") ?? EvolutionApiJson.ReadString(data, "status");
            var ownerJid = EvolutionApiJson.ReadString(data, "ownerJid") ?? EvolutionApiJson.ReadString(data, "owner");

            return new EvolutionInstanceInfo
            {
                InstanceName = EvolutionApiJson.ReadString(data, "name")
                               ?? EvolutionApiJson.ReadString(data, "instanceName")
                               ?? string.Empty,
                InstanceId = EvolutionApiJson.ReadString(data, "id") ?? EvolutionApiJson.ReadString(data, "instanceId"),
                State = EvolutionConnectionStates.Parse(rawState),
                RawState = rawState,
                OwnerJid = ownerJid,
                PhoneNumber = EvolutionPhoneNumber.FromJid(ownerJid) ?? EvolutionApiJson.ReadString(data, "number"),
                ProfileName = EvolutionApiJson.ReadString(data, "profileName"),
                ProfilePictureUrl = EvolutionApiJson.ReadString(data, "profilePicUrl")
                                    ?? EvolutionApiJson.ReadString(data, "profilePictureUrl"),
                Integration = EvolutionApiJson.ReadString(data, "integration"),
                Token = EvolutionApiJson.ReadString(data, "token") ?? EvolutionApiJson.ReadString(data, "apikey"),
                DisconnectionReasonCode = EvolutionApiJson.ReadInt(data, "disconnectionReasonCode"),
                DisconnectionAt = DateTimeOffset.TryParse(EvolutionApiJson.ReadString(data, "disconnectionAt"),
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var disconnectionAt)
                    ? disconnectionAt
                    : null
            };
        }

        private static EvolutionMessageResponse ReadMessage(JsonElement root, string content) => new()
        {
            MessageId = EvolutionApiJson.ReadString(root, "key", "id"),
            RemoteJid = EvolutionApiJson.ReadString(root, "key", "remoteJid"),
            Status = EvolutionApiJson.ReadString(root, "status"),
            MessageTimestamp = EvolutionApiJson.ReadLong(root, "messageTimestamp"),
            RawPayload = content
        };

        private Dictionary<string, object?> BuildWebhook(EvolutionWebhookConfig webhook)
        {
            var events = webhook.Events.Count > 0 ? webhook.Events : _options.DefaultWebhookEvents;

            return RemoveNulls(new Dictionary<string, object?>
            {
                ["enabled"] = webhook.Enabled,
                ["url"] = webhook.Url.Trim(),
                ["headers"] = webhook.Headers is { Count: > 0 } ? webhook.Headers : null,
                ["byEvents"] = webhook.ByEvents,
                ["base64"] = webhook.Base64,
                ["events"] = events.Select(EvolutionWebhookEvents.Normalize).Distinct().ToList()
            });
        }

        private async Task<ApiResponse<bool>> SendInstanceCommandAsync(
            HttpMethod method,
            string resource,
            string instanceName,
            EvolutionApiCredentials? credentials,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                return ApiResponse<bool>.CreateFail(MissingInstanceMessage);

            var selectedCredentials = ResolveCredentials(credentials);
            if (selectedCredentials is null)
                return ApiResponse<bool>.CreateFail(MissingCredentialsMessage);

            var response = await SendAsync(method, $"{resource}/{Escape(instanceName)}", selectedCredentials,
                body: null, cancellationToken);

            return ToBool(response);
        }

        private async Task<EvolutionHttpResponse> SendAsync(
            HttpMethod method,
            string relativeUrl,
            EvolutionApiCredentials credentials,
            object? body,
            CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, BuildUrl(credentials.BaseUrl, relativeUrl));
                message.Headers.TryAddWithoutValidation("apikey", credentials.ApiKey);

                if (body is not null)
                    message.Content = new StringContent(EvolutionApiJson.Serialize(body), Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient(EvolutionApiHttpClientNames.Default);
                using var response = await client.SendAsync(message, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                return response.IsSuccessStatusCode
                    ? new EvolutionHttpResponse(true, response.StatusCode, content, null)
                    : new EvolutionHttpResponse(false, response.StatusCode, content, DescribeError(response.StatusCode, content));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new EvolutionHttpResponse(false, null, string.Empty, TimeoutMessage);
            }
            catch (HttpRequestException ex)
            {
                return new EvolutionHttpResponse(false, null, string.Empty,
                    $"Falha na comunicacao com a Evolution API: {ex.Message}");
            }
        }

        private static ApiResponse<T> Read<T>(EvolutionHttpResponse response, Func<JsonElement, T> map)
        {
            if (!response.IsSuccess)
                return ApiResponse<T>.CreateFail(response.ErrorMessage!);

            if (string.IsNullOrWhiteSpace(response.Content))
                return ApiResponse<T>.CreateFail(InvalidResponseMessage);

            try
            {
                using var document = JsonDocument.Parse(response.Content);
                var data = map(document.RootElement);
                return data is null
                    ? ApiResponse<T>.CreateFail(InvalidResponseMessage)
                    : ApiResponse<T>.CreateSuccess(data);
            }
            catch (JsonException)
            {
                return ApiResponse<T>.CreateFail(InvalidResponseMessage);
            }
        }

        private static ApiResponse<bool> ToBool(EvolutionHttpResponse response) =>
            response.IsSuccess
                ? ApiResponse<bool>.CreateSuccess(true)
                : ApiResponse<bool>.CreateFail(response.ErrorMessage!);

        /// <summary>
        /// Erros da v2 chegam como <c>{ status, error, response: { message: [..] } }</c>; a
        /// mensagem pode ser texto ou lista.
        /// </summary>
        internal static string DescribeError(HttpStatusCode statusCode, string content)
        {
            var detail = ReadErrorMessage(content);
            if (detail is not null)
                return $"A Evolution API respondeu com o status {(int)statusCode}: {detail}";

            return string.IsNullOrWhiteSpace(content)
                ? $"A Evolution API respondeu com o status {(int)statusCode}."
                : $"A Evolution API respondeu com o status {(int)statusCode}: {content}";
        }

        private static string? ReadErrorMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                foreach (var path in new[] { new[] { "response", "message" }, ["message"], ["error"] })
                {
                    if (!EvolutionApiJson.TryGet(root, out var value, path))
                        continue;

                    var text = value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString(),
                        JsonValueKind.Array => string.Join(" | ", value.EnumerateArray().Select(Flatten).Where(x => x.Length > 0)),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }

        private static string Flatten(JsonElement element) =>
            element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Array => string.Join(" | ", element.EnumerateArray().Select(Flatten)),
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                _ => element.GetRawText()
            };

        private static bool IsInstanceNotFound(string content) =>
            content.Contains("does not exist", StringComparison.OrdinalIgnoreCase);

        private string? NormalizeNumber(string? number) =>
            EvolutionPhoneNumber.Normalize(number, _options.DefaultCountryCode);

        private static string StripDataUri(string media)
        {
            if (!media.StartsWith(DataUriPrefix, StringComparison.OrdinalIgnoreCase))
                return media;

            var separator = media.IndexOf(',', StringComparison.Ordinal);
            return separator < 0 ? media : media[(separator + 1)..];
        }

        private static Dictionary<string, object?> RemoveNulls(Dictionary<string, object?> values) =>
            values.Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value);

        private EvolutionApiCredentials? ResolveCredentials(EvolutionApiCredentials? credentials)
        {
            var selected = credentials is { IsComplete: true } ? credentials : _options.DefaultCredentials;
            return selected is { IsComplete: true } &&
                   Uri.TryCreate(selected.BaseUrl.Trim(), UriKind.Absolute, out _)
                ? selected
                : null;
        }

        private static string Escape(string instanceName) => Uri.EscapeDataString(instanceName.Trim());

        private static string BuildUrl(string baseUrl, string relativeUrl) =>
            $"{baseUrl.Trim().TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

        private const string MissingCredentialsMessage = "Endereco e chave da Evolution API nao foram configurados.";
        private const string MissingInstanceMessage = "Informe o nome da instancia.";
        private const string InvalidNumberMessage = "Informe um numero de WhatsApp valido, com DDD.";
        private const string InvalidResponseMessage = "Resposta invalida retornada pela Evolution API.";
        private const string TimeoutMessage = "Tempo limite excedido na comunicacao com a Evolution API.";

        private sealed record EvolutionHttpResponse(bool IsSuccess, HttpStatusCode? StatusCode, string Content, string? ErrorMessage);
    }
}
