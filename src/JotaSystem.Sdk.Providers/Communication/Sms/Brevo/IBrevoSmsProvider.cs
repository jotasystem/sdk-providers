using brevo_csharp.Model;

namespace JotaSystem.Sdk.Providers.Communication.Sms.Brevo
{
    public interface IBrevoSmsProvider
    {
        Task<SendSms> SendTransacSmsAsync(string recipient, string content, string? tag = null,
            bool unicodeEnabled = false, string? webUrl = null, string? organisationPrefix = null);
    }
}
