namespace JotaSystem.Sdk.Providers.Logistics.Shipping.Correios
{
    public class CorreiosOptions
    {
        public CorreiosCredentials? DefaultCredentials { get; set; }
        public string TokenBaseUrl { get; set; } = "https://api.correios.com.br/token/v1/";
        public string PriceBaseUrl { get; set; } = "https://api.correios.com.br/preco/v1/";
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan TokenExpirationMargin { get; set; } = TimeSpan.FromMinutes(1);
    }
}
