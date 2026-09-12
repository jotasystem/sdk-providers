namespace JotaSystem.Sdk.Providers.Payments.Cielo
{
    /// <summary>
    /// Nomes das credenciais que a Cielo consome do dicionario de segredos da integracao
    /// (<c>PaymentProviderContext.Secrets</c>). <c>MerchantId</c> e <c>ClientId</c> tambem
    /// podem vir da configuracao publica da integracao.
    /// </summary>
    public static class CieloSecretKeys
    {
        public const string MerchantId = "merchantId";
        public const string MerchantKey = "merchantKey";
        public const string ClientId = "clientId";
        public const string ClientSecret = "clientSecret";

        /// <summary>
        /// ClientId da API Link de Pagamento, gerado no Backoffice em E-commerce &gt; Link de
        /// Pagamento &gt; Configuracoes &gt; Credenciais da API. Nao e o do Silent Order Post.
        /// </summary>
        public const string LinkClientId = "linkClientId";

        /// <summary>ClientSecret da API Link de Pagamento, par do <see cref="LinkClientId"/>.</summary>
        public const string LinkClientSecret = "linkClientSecret";
    }
}
