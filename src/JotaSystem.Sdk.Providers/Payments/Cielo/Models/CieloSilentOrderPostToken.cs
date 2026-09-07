using System.Text.Json.Serialization;

namespace JotaSystem.Sdk.Providers.Payments.Cielo.Models
{
    /// <summary>
    /// Sessao do Silent Order Post. O <see cref="AccessToken"/> autoriza o script da Cielo,
    /// no navegador do comprador, a enviar o cartao direto para a Cielo e devolver um
    /// <c>PaymentToken</c> de uso unico. Ele nao da acesso a nenhuma operacao da loja.
    /// </summary>
    public class CieloSilentOrderPostToken
    {
        public string? MerchantId { get; set; }
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>Emissao no formato <c>yyyy-MM-ddTHH:mm:ss</c>.</summary>
        public string? Issued { get; set; }

        /// <summary>Expiracao no formato <c>yyyy-MM-ddTHH:mm:ss</c>.</summary>
        public string? ExpiresIn { get; set; }

        /// <summary>Endereco do script que deve ser carregado na pagina de checkout.</summary>
        [JsonIgnore]
        public string ScriptUrl { get; set; } = string.Empty;

        /// <summary><c>sandbox</c> ou <c>production</c>, conforme esperado pelo script.</summary>
        [JsonIgnore]
        public string Environment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resposta do autenticador OAuth2 usado pelo Silent Order Post.
    /// </summary>
    internal class CieloAuthToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
