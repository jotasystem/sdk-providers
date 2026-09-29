using brevo_csharp.Api;
using brevo_csharp.Model;

namespace JotaSystem.Sdk.Providers.Communication.Sms.Brevo
{
    public class BrevoSmsProvider(BrevoSmsOptions options) : IBrevoSmsProvider
    {
        public async Task<SendSms> SendTransacSmsAsync(string recipient, string content, string? tag = null,
            bool unicodeEnabled = false, string? webUrl = null, string? organisationPrefix = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
            ArgumentException.ThrowIfNullOrWhiteSpace(content);

            var config = new brevo_csharp.Client.Configuration
            {
                ApiKey = { ["api-key"] = options.ApiKey }
            };

            var apiInstance = new TransactionalSMSApi(config);
            var sendSms = new SendTransacSms(
                sender: options.Sender,
                recipient: recipient,
                content: content,
                tag: tag is null ? null : new SendTransacSmsTag(tag),
                webUrl: webUrl,
                unicodeEnabled: unicodeEnabled,
                organisationPrefix: organisationPrefix);

            return await apiInstance.SendTransacSmsAsync(sendSms);
        }
    }
}
