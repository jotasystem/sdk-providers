namespace JotaSystem.Sdk.Providers.Communication.Email.SendGrid
{
    public class SendGridOptions
    {
        public string ApiKey { get; set; } = default!;
        public string FromName { get; set; } = default!;
        public string FromEmail { get; set; } = default!;
    }
}
