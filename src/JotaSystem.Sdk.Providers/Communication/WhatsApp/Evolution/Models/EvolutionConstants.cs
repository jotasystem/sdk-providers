namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Eventos de webhook da Evolution API, no formato aceito em <c>webhook.events</c>.
    /// No corpo da notificacao o mesmo evento chega minusculo e com ponto
    /// (<c>connection.update</c>); <see cref="EvolutionWebhookParser"/> normaliza para estes nomes.
    /// </summary>
    public static class EvolutionWebhookEvents
    {
        public const string ApplicationStartup = "APPLICATION_STARTUP";
        public const string QrCodeUpdated = "QRCODE_UPDATED";
        public const string ConnectionUpdate = "CONNECTION_UPDATE";
        public const string MessagesSet = "MESSAGES_SET";
        public const string MessagesUpsert = "MESSAGES_UPSERT";
        public const string MessagesUpdate = "MESSAGES_UPDATE";
        public const string MessagesDelete = "MESSAGES_DELETE";
        public const string SendMessage = "SEND_MESSAGE";
        public const string ContactsUpsert = "CONTACTS_UPSERT";
        public const string PresenceUpdate = "PRESENCE_UPDATE";
        public const string ChatsUpsert = "CHATS_UPSERT";
        public const string GroupsUpsert = "GROUPS_UPSERT";
        public const string Call = "CALL";
        public const string LogoutInstance = "LOGOUT_INSTANCE";
        public const string RemoveInstance = "REMOVE_INSTANCE";

        /// <summary>Converte <c>connection.update</c> ou <c>connection-update</c> em <c>CONNECTION_UPDATE</c>.</summary>
        public static string Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace('.', '_').Replace('-', '_').ToUpperInvariant();
    }

    /// <summary>Tipos aceitos em <c>mediatype</c> no envio de midia.</summary>
    public static class EvolutionMediaTypes
    {
        public const string Image = "image";
        public const string Video = "video";
        public const string Audio = "audio";
        public const string Document = "document";
    }

    /// <summary>Estado da sessao do WhatsApp na instancia.</summary>
    public enum EvolutionConnectionStateEnum
    {
        Unknown = 0,

        /// <summary>Sessao encerrada ou nunca pareada: e preciso ler o QR Code.</summary>
        Close = 1,

        /// <summary>Aguardando a leitura do QR Code ou o pareamento.</summary>
        Connecting = 2,

        /// <summary>Numero conectado e pronto para enviar mensagens.</summary>
        Open = 3
    }

    public static class EvolutionConnectionStates
    {
        public static EvolutionConnectionStateEnum Parse(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "open" or "connected" => EvolutionConnectionStateEnum.Open,
                "connecting" => EvolutionConnectionStateEnum.Connecting,
                "close" or "closed" or "refused" or "disconnected" => EvolutionConnectionStateEnum.Close,
                _ => EvolutionConnectionStateEnum.Unknown
            };
    }
}
