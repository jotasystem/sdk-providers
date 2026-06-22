using SendPulse.SDK.Models;

namespace JotaSystem.Sdk.Providers.Communication.Email.SendPulse
{
    public interface ISendPulseProvider
    {
        Task<bool> SendTransacEmailAsync(List<EmailAddress> tos, string subject, string body, bool isHtml = true);
    }
}
