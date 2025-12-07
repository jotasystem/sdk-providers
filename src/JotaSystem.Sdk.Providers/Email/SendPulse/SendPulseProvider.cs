using SendPulse.SDK;
using SendPulse.SDK.Models;

namespace JotaSystem.Sdk.Providers.Email.SendPulse
{
    public class SendPulseProvider(SendPulseOptions options) : ISendPulseProvider
    {
        public async Task<bool> SendTransacEmailAsync(List<EmailAddress> tos, string subject, string body, bool isHtml = true)
        {
            using var sendPulse = new SendPulseService(options.ClientId, options.ClientSecret);

            var emailData = new EmailData
            {
                Subject = subject,
                From = new EmailAddress
                {
                    Name = options.FromName,
                    Address = options.FromEmail
                },
                To = tos
            };

            if (isHtml)
                emailData.HTML = body;
            else
                emailData.Text = body;

            return await sendPulse.SendEmailAsync(emailData);
        }
    }
}