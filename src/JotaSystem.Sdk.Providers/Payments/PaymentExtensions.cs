using JotaSystem.Sdk.Providers.Payments.Cielo;
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
        /// Registra a integracao com a API E-commerce da Cielo, disponivel tanto pelo
        /// contrato de gateway (<see cref="IPaymentGatewayProvider"/>, chave <c>cielo</c>)
        /// quanto pelo contrato completo da Cielo (<see cref="ICieloProvider"/>).
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
            builder.Services.AddScoped<IPaymentGatewayProvider, CieloPaymentGatewayProvider>();

            return builder;
        }
    }
}
