namespace JotaSystem.Sdk.Providers.Communication.Email.Brevo
{
    public class BrevoOptions
    {
        public string ApiKey { get; set; } = default!;
        public string FromName { get; set; } = default!;
        public string FromEmail { get; set; } = default!;
    }
}