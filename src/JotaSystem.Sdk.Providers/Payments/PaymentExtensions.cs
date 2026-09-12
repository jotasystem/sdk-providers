using JotaSystem.Sdk.Providers.Payments.Cielo;
using JotaSystem.Sdk.Providers.Payments.Cielo.Link;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers.Payments
{
    public static class PaymentExtensions
    {
        public static JotaSystemSdkBuilder AddPayments(this JotaSystemSdkBuilder builder)
        {
            builder.Services.AddScoped<IPaymentGatewayProviderResolver, PaymentGatewayProviderResolver>();
            return builder;
        }

        /// <summary>
        /// Registra as integracoes com a Cielo — a API E-commerce e a API Link de Pagamento —
        /// disponiveis tanto pelo contrato de gateway (<see cref="IPaymentGatewayProvider"/>,
        /// chave <c>cielo</c>) quanto pelos contratos completos de cada API
        /// (<see cref="ICieloProvider"/> e <see cref="ICieloLinkProvider"/>).
        /// </summary>
        public static JotaSystemSdkBuilder AddCielo(this JotaSystemSdkBuilder builder, Action<CieloOptions>? configure = null)
        {
            var options = new CieloOptions();
            configure?.Invoke(options);

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ICieloAuthTokenCache, CieloAuthTokenCache>();

            builder.Services.AddHttpClient(CieloHttpClientNames.Default, client =>
            {
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            builder.Services.AddScoped<ICieloProvider, CieloProvider>();
            builder.Services.AddScoped<ICieloLinkProvider, CieloLinkProvider>();
            builder.Services.AddScoped<IPaymentGatewayProvider, CieloPaymentGatewayProvider>();

            return builder;
        }
    }
}
