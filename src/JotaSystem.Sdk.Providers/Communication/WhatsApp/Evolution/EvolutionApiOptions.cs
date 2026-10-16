namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Configuracao da integracao com a Evolution API (WhatsApp nao oficial, via Baileys).
    /// </summary>
    public class EvolutionApiOptions
    {
        /// <summary>Servidor usado quando a chamada nao informa credenciais proprias.</summary>
        public EvolutionApiCredentials? DefaultCredentials { get; set; }

        /// <summary>
        /// Motor da instancia criada. <c>WHATSAPP-BAILEYS</c> conecta por QR Code;
        /// <c>WHATSAPP-BUSINESS</c> usa a API oficial da Meta e exige token proprio.
        /// </summary>
        public string Integration { get; set; } = EvolutionIntegrationTypes.Baileys;

        /// <summary>Eventos assinados quando o webhook e configurado sem lista propria.</summary>
        public IList<string> DefaultWebhookEvents { get; } =
        [
            EvolutionWebhookEvents.QrCodeUpdated,
            EvolutionWebhookEvents.ConnectionUpdate,
            EvolutionWebhookEvents.MessagesUpsert,
            EvolutionWebhookEvents.MessagesUpdate,
            EvolutionWebhookEvents.SendMessage
        ];

        /// <summary>DDI aplicado a numeros nacionais (10 ou 11 digitos) antes do envio.</summary>
        public string DefaultCountryCode { get; set; } = "55";

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    public static class EvolutionIntegrationTypes
    {
        public const string Baileys = "WHATSAPP-BAILEYS";
        public const string Business = "WHATSAPP-BUSINESS";
    }
}
