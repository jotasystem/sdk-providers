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

        /// <summary>Ambiente em que as credenciais sao validas.</summary>
        public CieloEnvironmentEnum Environment { get; set; } = CieloEnvironmentEnum.Sandbox;
    }
}
