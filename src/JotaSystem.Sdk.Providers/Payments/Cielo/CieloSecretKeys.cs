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
    }
}
