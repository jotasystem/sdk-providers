namespace JotaSystem.Sdk.Providers.Email.Smtp
{
    public class SmtpOptions
    {
        public string Host { get; set; } = default!;
        public int Port { get; set; }
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
        public bool EnableSsl { get; set; }
        public string FromName { get; set; } = default!;
        public string FromEmail { get; set; } = default!;
        public bool UseDefaultCredentials { get; set; } = false;
        public int Timeout { get; set; } = 20000;
    }
}