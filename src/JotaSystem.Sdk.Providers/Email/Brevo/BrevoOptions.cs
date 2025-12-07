namespace JotaSystem.Sdk.Providers.Email.Brevo
{
    public class BrevoOptions
    {
        public string ApiKey { get; set; } = default!;
        public string FromName { get; set; } = default!;
        public string FromEmail { get; set; } = default!;
    }
}