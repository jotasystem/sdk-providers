namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution.Models
{
    /// <summary>
    /// Notificacao recebida do servidor, ja normalizada. Apenas os campos do evento
    /// recebido vem preenchidos.
    /// </summary>
    public class EvolutionWebhookEvent
    {
        /// <summary>Evento normalizado. Ver <see cref="EvolutionWebhookEvents"/>.</summary>
        public string Event { get; set; } = string.Empty;

        public string? InstanceName { get; set; }

        /// <summary>Token da instancia enviado pelo servidor junto da notificacao.</summary>
        public string? ApiKey { get; set; }

        public string? ServerUrl { get; set; }
        public DateTimeOffset? OccurredAt { get; set; }

        /// <summary>Preenchido em <see cref="EvolutionWebhookEvents.ConnectionUpdate"/>.</summary>
        public EvolutionConnectionStateEnum? State { get; set; }

        /// <summary>Codigo do motivo informado pelo WhatsApp (ex.: 401 quando o aparelho desconectou).</summary>
        public int? StatusReason { get; set; }

        /// <summary>JID do numero conectado.</summary>
        public string? OwnerJid { get; set; }

        /// <summary>Numero conectado, apenas digitos.</summary>
        public string? PhoneNumber { get; set; }

        public string? ProfileName { get; set; }
        public string? ProfilePictureUrl { get; set; }

        /// <summary>Preenchido em <see cref="EvolutionWebhookEvents.QrCodeUpdated"/>.</summary>
        public EvolutionQrCode? QrCode { get; set; }

        /// <summary>Preenchido nos eventos de mensagem.</summary>
        public EvolutionWebhookMessage? Message { get; set; }

        public string RawPayload { get; set; } = string.Empty;
    }

    public class EvolutionWebhookMessage
    {
        public string? MessageId { get; set; }
        public string? RemoteJid { get; set; }

        /// <summary>Numero do contato, apenas digitos, extraido do <see cref="RemoteJid"/>.</summary>
        public string? PhoneNumber { get; set; }

        public bool FromMe { get; set; }
        public bool IsGroup { get; set; }
        public string? PushName { get; set; }
        public string? MessageType { get; set; }

        /// <summary>Texto da mensagem, quando ela tem texto (conversa, legenda ou resposta de botao).</summary>
        public string? Text { get; set; }

        /// <summary>Situacao da entrega em <c>MESSAGES_UPDATE</c>: <c>SERVER_ACK</c>, <c>DELIVERY_ACK</c>, <c>READ</c>...</summary>
        public string? Status { get; set; }

        public long? Timestamp { get; set; }
    }
}
