using System.Net;
using System.Net.Mail;

namespace JotaSystem.Sdk.Providers.Email.Smtp
{
    public class SmtpProvider(SmtpOptions options) : ISmtpProvider
    {
        public async Task<bool> SendAsync(List<(string Name, string Email)> tos, string subject, string body, bool isHtml = true,
            List<(string Name, string Email)>? ccs = null, List<(string Name, string Email)>? bccs = null, string? replyToEmail = null, string? replyToName = null,
            List<(string FileName, byte[] Bytes)>? attachments = null, MailPriority priority = MailPriority.Normal, Dictionary<string, string>? headers = null)
        {
            if (string.IsNullOrWhiteSpace(options.Host))
                throw new InvalidOperationException("SMTP host is required.");

            var smtpClient = new SmtpClient(options.Host)
            {
                Port = options.Port,
                EnableSsl = options.EnableSsl,
                Timeout = options.Timeout,
                UseDefaultCredentials = options.UseDefaultCredentials
            };

            if (!options.UseDefaultCredentials)
                smtpClient.Credentials = new NetworkCredential(options.Username, options.Password);

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromEmail, options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                Priority = priority
            };

            foreach (var to in tos)
                mailMessage.To.Add(new MailAddress(to.Email, to.Name));

            if (ccs != null)
            {
                foreach (var cc in ccs)
                    mailMessage.CC.Add(new MailAddress(cc.Email, cc.Name));
            }

            if (bccs != null)
            {
                foreach (var bcc in bccs)
                    mailMessage.Bcc.Add(new MailAddress(bcc.Email, bcc.Name));
            }

            if (replyToEmail != null)
                mailMessage.ReplyToList.Add(new MailAddress(replyToEmail, replyToName));

            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    var stream = new MemoryStream(att.Bytes);
                    mailMessage.Attachments.Add(new Attachment(stream, att.FileName));
                }
            }

            if (headers != null)
            {
                foreach (var kv in headers)
                    mailMessage.Headers.Add(kv.Key, kv.Value);
            }

            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
    }
}
