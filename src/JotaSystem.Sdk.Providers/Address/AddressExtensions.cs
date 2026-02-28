using JotaSystem.Sdk.Providers.Address.BrasilApi;
using JotaSystem.Sdk.Providers.Address.ViaCep;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers.Address
{
    public static class AddressExtensions
    {
        public static JotaSystemSdkBuilder AddViaCep(this JotaSystemSdkBuilder builder)
        {
            builder.Services.AddHttpClient<IViaCepProvider, ViaCepProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            return builder;
        }

        public static JotaSystemSdkBuilder AddBrasilApi(this JotaSystemSdkBuilder builder)
        {
            builder.Services.AddHttpClient<IBrasilApiProvider, BrasilApiProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            return builder;
        }

    }
}