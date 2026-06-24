namespace JotaSystem.Sdk.Providers.Logistics.Shipping.MelhorEnvio
{
    public class MelhorEnvioOptions
    {
        public string BaseUrl { get; set; } = MelhorEnvioEnvironment.SandboxBaseUrl;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public string UserAgent { get; set; } = "JotaSystemSdk/1.0 (suporte@jotasystem.com.br)";
        public string? AccessToken { get; set; }
    }

    public static class MelhorEnvioEnvironment
    {
        public const string SandboxBaseUrl = "https://sandbox.melhorenvio.com.br/";
        public const string ProductionBaseUrl = "https://melhorenvio.com.br/";
    }
}
