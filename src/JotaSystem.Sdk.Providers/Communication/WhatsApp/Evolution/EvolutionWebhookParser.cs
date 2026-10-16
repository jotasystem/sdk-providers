using JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models;
using System.Globalization;
using System.Text.Json;

namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Le a notificacao enviada pela Evolution API para o webhook da instancia.
    /// </summary>
    /// <remarks>
    /// Formato (v2, <c>byEvents</c> desligado):
    /// <c>{ "event": "connection.update", "instance": "nome", "data": { ... }, "date_time": "...", "server_url": "...", "apikey": "token" }</c>.
    /// </remarks>
    public static class EvolutionWebhookParser
    {
        public static ApiResponse<EvolutionWebhookEvent> Parse(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return ApiResponse<EvolutionWebhookEvent>.CreateFail("Conteudo da notificacao nao informado.");

            try
            {
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return ApiResponse<EvolutionWebhookEvent>.CreateFail("Notificacao da Evolution API em formato invalido.");

                var eventName = EvolutionWebhookEvents.Normalize(EvolutionApiJson.ReadString(root, "event"));
                if (eventName.Length == 0)
                    return ApiResponse<EvolutionWebhookEvent>.CreateFail("A notificacao nao informa o evento.");

                var result = new EvolutionWebhookEvent
                {
                    Event = eventName,
                    InstanceName = ReadInstanceName(root),
                    ApiKey = EvolutionApiJson.ReadString(root, "apikey"),
                    ServerUrl = EvolutionApiJson.ReadString(root, "server_url"),
                    OccurredAt = ReadDate(EvolutionApiJson.ReadString(root, "date_time")),
                    RawPayload = payload
                };

                if (EvolutionApiJson.TryGet(root, out var data, "data"))
                    Fill(result, data);

                return ApiResponse<EvolutionWebhookEvent>.CreateSuccess(result);
            }
            catch (JsonException)
            {
                return ApiResponse<EvolutionWebhookEvent>.CreateFail("Notificacao da Evolution API em formato invalido.");
            }
        }

        private static string? ReadInstanceName(JsonElement root)
        {
            if (EvolutionApiJson.TryGet(root, out var instance, "instance") && instance.ValueKind == JsonValueKind.Object)
                return EvolutionApiJson.ReadString(instance, "instanceName") ?? EvolutionApiJson.ReadString(instance, "name");

            return EvolutionApiJson.ReadString(root, "instance");
        }

        private static void Fill(EvolutionWebhookEvent result, JsonElement data)
        {
            switch (result.Event)
            {
                case EvolutionWebhookEvents.ConnectionUpdate:
                    result.State = EvolutionConnectionStates.Parse(EvolutionApiJson.ReadString(data, "state"));
                    result.StatusReason = EvolutionApiJson.ReadInt(data, "statusReason");
                    result.OwnerJid = EvolutionApiJson.ReadString(data, "wuid");
                    result.PhoneNumber = EvolutionPhoneNumber.FromJid(result.OwnerJid);
                    result.ProfileName = EvolutionApiJson.ReadString(data, "profileName");
                    result.ProfilePictureUrl = EvolutionApiJson.ReadString(data, "profilePictureUrl");
                    break;

                case EvolutionWebhookEvents.QrCodeUpdated:
                    var qrCode = EvolutionApiJson.TryGet(data, out var qr, "qrcode") ? qr : data;
                    result.QrCode = EvolutionApiProvider.ReadQrCode(qrCode);
                    break;

                case EvolutionWebhookEvents.MessagesUpsert:
                case EvolutionWebhookEvents.SendMessage:
                case EvolutionWebhookEvents.MessagesUpdate:
                    // MESSAGES_UPSERT pode chegar com uma lista em data.messages.
                    var message = data;
                    if (EvolutionApiJson.TryGet(data, out var messages, "messages") &&
                        messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0)
                        message = messages[0];
                    else if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                        message = data[0];

                    result.Message = ReadMessage(message);
                    break;
            }
        }

        private static EvolutionWebhookMessage ReadMessage(JsonElement data)
        {
            var remoteJid = EvolutionApiJson.ReadString(data, "key", "remoteJid")
                            ?? EvolutionApiJson.ReadString(data, "remoteJid");

            return new EvolutionWebhookMessage
            {
                MessageId = EvolutionApiJson.ReadString(data, "key", "id")
                            ?? EvolutionApiJson.ReadString(data, "keyId")
                            ?? EvolutionApiJson.ReadString(data, "messageId"),
                RemoteJid = remoteJid,
                PhoneNumber = EvolutionPhoneNumber.FromJid(remoteJid),
                IsGroup = EvolutionPhoneNumber.IsGroupJid(remoteJid),
                FromMe = EvolutionApiJson.ReadBool(data, "key", "fromMe")
                         ?? EvolutionApiJson.ReadBool(data, "fromMe")
                         ?? false,
                PushName = EvolutionApiJson.ReadString(data, "pushName"),
                MessageType = EvolutionApiJson.ReadString(data, "messageType"),
                Text = ReadText(data),
                Status = EvolutionApiJson.ReadString(data, "status"),
                Timestamp = EvolutionApiJson.ReadLong(data, "messageTimestamp")
            };
        }

        private static string? ReadText(JsonElement data)
        {
            if (!EvolutionApiJson.TryGet(data, out var message, "message"))
                return null;

            return EvolutionApiJson.ReadString(message, "conversation")
                   ?? EvolutionApiJson.ReadString(message, "extendedTextMessage", "text")
                   ?? EvolutionApiJson.ReadString(message, "imageMessage", "caption")
                   ?? EvolutionApiJson.ReadString(message, "videoMessage", "caption")
                   ?? EvolutionApiJson.ReadString(message, "documentMessage", "caption")
                   ?? EvolutionApiJson.ReadString(message, "buttonsResponseMessage", "selectedDisplayText")
                   ?? EvolutionApiJson.ReadString(message, "listResponseMessage", "title")
                   ?? EvolutionApiJson.ReadString(message, "templateButtonReplyMessage", "selectedDisplayText");
        }

        private static DateTimeOffset? ReadDate(string? value) =>
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
                ? date
                : null;
    }
}
