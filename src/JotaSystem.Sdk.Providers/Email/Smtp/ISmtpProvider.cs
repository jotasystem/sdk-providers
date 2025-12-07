using System.Net.Mail;

namespace JotaSystem.Sdk.Providers.Email.Smtp
{
    public interface ISmtpProvider
    {
        Task<bool> SendAsync(List<(string Name, string Email)> tos, string subject, string body, bool isHtml = true,
            List<(string Name, string Email)>? ccs = null, List<(string Name, string Email)>? bccs = null, string? replyToEmail = null, string? replyToName = null,
            List<(string FileName, byte[] Bytes)>? attachments = null, MailPriority priority = MailPriority.Normal, Dictionary<string, string>? headers = null);
    }
}
