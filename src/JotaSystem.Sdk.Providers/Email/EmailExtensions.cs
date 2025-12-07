using JotaSystem.Sdk.Providers.Email.Brevo;
using JotaSystem.Sdk.Providers.Email.SendGrid;
using JotaSystem.Sdk.Providers.Email.SendPulse;
using JotaSystem.Sdk.Providers.Email.Smtp;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers.Email
{
    public static class EmailExtensions
    {
        public static JotaSystemSdkBuilder AddBrevo(this JotaSystemSdkBuilder builder, Action<BrevoOptions> configure)
        {
            var options = new BrevoOptions();
            configure(options);

            builder.Services.AddSingleton<IBrevoProvider>(new BrevoProvider(options));
            return builder;
        }

        public static JotaSystemSdkBuilder AddSendGrid(this JotaSystemSdkBuilder builder, Action<SendGridOptions> configure)
        {
            var options = new SendGridOptions();
            configure(options);

            builder.Services.AddSingleton<ISendGridProvider>(new SendGridProvider(options));
            return builder;
        }

        public static JotaSystemSdkBuilder AddSendPulse(this JotaSystemSdkBuilder builder, Action<SendPulseOptions> configure)
        {
            var options = new SendPulseOptions();
            configure(options);

            builder.Services.AddSingleton<ISendPulseProvider>(new SendPulseProvider(options));
            return builder;
        }

        public static JotaSystemSdkBuilder AddSmtp(this JotaSystemSdkBuilder builder, Action<SmtpOptions> configure)
        {
            var options = new SmtpOptions();
            configure(options);

            builder.Services.AddSingleton<ISmtpProvider>(new SmtpProvider(options));
            return builder;
        }
    }
}