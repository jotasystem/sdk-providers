using brevo_csharp.Api;
using brevo_csharp.Model;

namespace JotaSystem.Sdk.Providers.Email.Brevo
{
    public class BrevoProvider(BrevoOptions options) : IBrevoProvider
    {
        public async Task<CreateSmtpEmail> SendTransacEmailAsync(List<SendSmtpEmailTo> tos, string subject, string body, bool isHtml = true,
            List<SendSmtpEmailCc>? ccs = null, List<SendSmtpEmailBcc>? bccs = null, List<string>? tags = null, Dictionary<string, object>? parameters = null,
            List<SendSmtpEmailAttachment>? attachments = null, string? replyToEmail = null, string? replyToName = null, int? templateId = null)
        {
            if (tos == null || tos.Count == 0)
                throw new ArgumentException("É necessário informar ao menos um destinatário.");

            var config = new brevo_csharp.Client.Configuration
            {
                ApiKey = { ["api-key"] = options.ApiKey }
            };

            var apiInstance = new TransactionalEmailsApi(config);

            var sendEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender { Name = options.FromName, Email = options.FromEmail },
                To = tos,
                Subject = subject,
                ReplyTo = new SendSmtpEmailReplyTo
                {
                    Email = replyToEmail ?? options.FromEmail,
                    Name = replyToName ?? options.FromName
                }
            };

            if (isHtml) sendEmail.HtmlContent = body;
            else sendEmail.TextContent = body;

            if (ccs != null) sendEmail.Cc = ccs;
            if (bccs != null) sendEmail.Bcc = bccs;
            if (tags != null) sendEmail.Tags = tags;
            if (parameters != null) sendEmail.Params = parameters;
            if (attachments != null) sendEmail.Attachment = attachments;

            if (templateId.HasValue)
            {
                sendEmail.TemplateId = templateId;
                sendEmail.HtmlContent = null;
                sendEmail.TextContent = null;
            }

            return await apiInstance.SendTransacEmailAsync(sendEmail);
        }
    }
}