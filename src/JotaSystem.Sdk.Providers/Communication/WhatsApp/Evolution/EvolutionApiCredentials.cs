namespace JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution
{
    /// <summary>
    /// Acesso a um servidor da Evolution API. O servidor costuma ser compartilhado por
    /// varias instancias (numeros de WhatsApp), todas autenticadas pela mesma chave global.
    /// </summary>
    public class EvolutionApiCredentials
    {
        /// <summary>Endereco do servidor, ex.: <c>https://evolution.minhaempresa.com.br</c>.</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Chave enviada no header <c>apikey</c>. A chave global
        /// (<c>AUTHENTICATION_API_KEY</c> do servidor) libera todas as operacoes; o token de
        /// uma instancia libera apenas as operacoes daquela instancia.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
