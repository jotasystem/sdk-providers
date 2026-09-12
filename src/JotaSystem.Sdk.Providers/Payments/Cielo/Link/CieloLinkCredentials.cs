namespace JotaSystem.Sdk.Providers.Payments.Cielo.Link
{
    /// <summary>
    /// Credenciais da API Link de Pagamento. Sao geradas no Backoffice Cielo em
    /// E-commerce &gt; Link de Pagamento &gt; Configuracoes &gt; Credenciais da API e nao se
    /// confundem com o <c>MerchantId</c> e o <c>MerchantKey</c> da API E-commerce.
    /// </summary>
    /// <remarks>
    /// O Link de Pagamento nao tem ambiente de sandbox: o teste e feito ligando o modo de
    /// teste da loja no Backoffice, com as mesmas credenciais e a mesma URL de producao.
    /// </remarks>
    public sealed class CieloLinkCredentials
    {
        /// <summary>ClientId gerado no Backoffice para a API Link de Pagamento.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>ClientSecret gerado junto com o <see cref="ClientId"/>.</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Indica que as credenciais estao completas.</summary>
        public bool IsFilled =>
            !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
