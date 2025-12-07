namespace JotaSystem.Sdk.Providers.Email.SendPulse
{
    public class SendPulseOptions
    {
        public string ClientId { get; set; } = default!;
        public string ClientSecret { get; set; } = default!;
        public string FromName { get; set; } = default!;
        public string FromEmail { get; set; } = default!;
    }
}