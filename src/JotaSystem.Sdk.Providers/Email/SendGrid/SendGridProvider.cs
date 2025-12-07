using SendGrid;
using SendGrid.Helpers.Mail;

namespace JotaSystem.Sdk.Providers.Email.SendGrid
{
    public class SendGridProvider(SendGridOptions options) : ISendGridProvider
    {
        public async Task<Response> SendTransacEmailAsync(List<EmailAddress> tos, string subject, string body, bool isHtml = true,
            List<EmailAddress>? ccs = null, List<EmailAddress>? bccs = null, List<string>? categories = null, Dictionary<string, object>? parameters = null,
            List<(string FileName, byte[] Bytes)>? attachments = null, string? replyToEmail = null, string? replyToName = null, string? templateId = null)
        {
            var client = new SendGridClient(options.ApiKey);

            var msg = new SendGridMessage
            {
                From = new EmailAddress(options.FromEmail, options.FromName),
                Subject = subject
            };

            if (replyToEmail != null)
                msg.ReplyTo = new EmailAddress(replyToEmail, replyToName);

            msg.AddTos(tos);

            if (ccs != null && ccs.Count > 0) msg.AddCcs(ccs);
            if (bccs != null && bccs.Count > 0) msg.AddBccs(bccs);

            if (templateId is null)
            {
                if (isHtml) msg.HtmlContent = body;
                else msg.PlainTextContent = body;
            }
            else
            {
                // Com template (Ignora body)
                msg.TemplateId = templateId;

                if (parameters != null) msg.SetTemplateData(parameters);
            }

            if (templateId == null && parameters != null)
            {
                foreach (var kv in parameters)
                {
                    msg.AddSubstitution($"-{kv.Key}-", kv.Value.ToString());
                }
            }

            if (categories != null)
                msg.AddCategories(categories);

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    msg.AddAttachment(
                        file.FileName,
                        Convert.ToBase64String(file.Bytes)
                    );
                }
            }

            return await client.SendEmailAsync(msg);
        }
    }
}