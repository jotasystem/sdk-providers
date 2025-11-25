namespace JotaSystem.Sdk.Providers.Services.BrasilApi.Models
{
    public class BrasilApiBankResponse
    {
        public string Ispb { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Code { get; set; }
        public string FullName { get; set; } = string.Empty;
    }
}