using JotaSystem.Sdk.Providers.Communication.Email.Brevo;
using JotaSystem.Sdk.Providers.Communication.Email.SendGrid;
using JotaSystem.Sdk.Providers.Communication.Email.SendPulse;
using JotaSystem.Sdk.Providers.Communication.Email.Smtp;
using JotaSystem.Sdk.Providers.Communication.Sms.Brevo;
using JotaSystem.Sdk.Providers.Communication.WhatsApp.Evolution;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers.Communication
{
    public static class CommunicationExtensions
    {
        public static JotaSystemSdkBuilder AddBrevo(this JotaSystemSdkBuilder builder, Action<BrevoOptions> configure)
        {
            var options = new BrevoOptions();
            configure(options);

            builder.Services.AddSingleton<IBrevoProvider>(new BrevoProvider(options));
            return builder;
        }

        public static JotaSystemSdkBuilder AddBrevoSms(this JotaSystemSdkBuilder builder, Action<BrevoSmsOptions> configure)
        {
            var options = new BrevoSmsOptions();
            configure(options);

            builder.Services.AddSingleton<IBrevoSmsProvider>(new BrevoSmsProvider(options));
            return builder;
        }

        /// <summary>
        /// Registra a integracao com a Evolution API (WhatsApp). O servidor padrao e opcional:
        /// sem ele, cada chamada precisa informar as proprias <see cref="EvolutionApiCredentials"/>.
        /// </summary>
        public static JotaSystemSdkBuilder AddEvolutionApi(this JotaSystemSdkBuilder builder, Action<EvolutionApiOptions>? configure = null)
        {
            var options = new EvolutionApiOptions();
            configure?.Invoke(options);

            builder.Services.AddSingleton(options);
            builder.Services.AddHttpClient(EvolutionApiHttpClientNames.Default, client =>
            {
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            builder.Services.AddScoped<IEvolutionApiProvider, EvolutionApiProvider>();
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
