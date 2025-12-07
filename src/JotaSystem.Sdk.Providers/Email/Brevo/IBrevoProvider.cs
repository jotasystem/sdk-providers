using brevo_csharp.Model;

namespace JotaSystem.Sdk.Providers.Email.Brevo
{
    /// <summary>
    /// https://www.brevo.com/
    /// </summary>
    public interface IBrevoProvider
    {
        Task<CreateSmtpEmail> SendTransacEmailAsync(List<SendSmtpEmailTo> tos, string subject, string body, bool isHtml = true,
            List<SendSmtpEmailCc>? ccs = null, List<SendSmtpEmailBcc>? bccs = null, List<string>? tags = null, Dictionary<string, object>? parameters = null,
            List<SendSmtpEmailAttachment>? attachments = null, string? replyToEmail = null, string? replyToName = null, int? templateId = null);
    }
}