namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Credenciais de acesso de uma loja na API E-commerce da Cielo.
    /// </summary>
    public class CieloCredentials
    {
        /// <summary>Identificador da loja na Cielo (GUID).</summary>
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>Chave publica de autenticacao da loja na Cielo.</summary>
        public string MerchantKey { get; set; } = string.Empty;

        /// <summary>
        /// ClientId do OAuth2, usado apenas para abrir a sessao do Silent Order Post.
        /// Fornecido pela Cielo separadamente do <see cref="MerchantId"/>.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>ClientSecret do OAuth2, par do <see cref="ClientId"/>.</summary>
        public string? ClientSecret { get; set; }

        /// <summary>Ambiente em que as credenciais sao validas.</summary>
        public CieloEnvironmentEnum Environment { get; set; } = CieloEnvironmentEnum.Sandbox;

        /// <summary>Indica se a loja pode abrir sessoes do Silent Order Post.</summary>
        public bool SupportsSilentOrderPost =>
            !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
