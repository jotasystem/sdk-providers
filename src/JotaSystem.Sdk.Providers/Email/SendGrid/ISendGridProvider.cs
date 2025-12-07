using SendGrid;
using SendGrid.Helpers.Mail;

namespace JotaSystem.Sdk.Providers.Email.SendGrid
{
    public interface ISendGridProvider
    {
        Task<Response> SendTransacEmailAsync(List<EmailAddress> tos, string subject, string body, bool isHtml = true,
            List<EmailAddress>? ccs = null, List<EmailAddress>? bccs = null, List<string>? categories = null, Dictionary<string, object>? parameters = null,
            List<(string FileName, byte[] Bytes)>? attachments = null, string? replyToEmail = null, string? replyToName = null, string? templateId = null);
    }
}