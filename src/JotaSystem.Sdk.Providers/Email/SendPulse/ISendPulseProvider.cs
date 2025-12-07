using SendPulse.SDK.Models;

namespace JotaSystem.Sdk.Providers.Email.SendPulse
{
    public interface ISendPulseProvider
    {
        Task<bool> SendTransacEmailAsync(List<EmailAddress> tos, string subject, string body, bool isHtml = true);
    }
}
