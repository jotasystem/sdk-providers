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
    }
}
