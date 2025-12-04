using JotaSystem.Sdk.Providers.Address;
using JotaSystem.Sdk.Providers.Address.BrasilApi;
using JotaSystem.Sdk.Providers.Address.ViaCep;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registra todos os Providers disponíveis no container de injeção de dependência.
        /// </summary>
        public static IServiceCollection AddJotaSystemProviders(this IServiceCollection services)
        {
            // ViaCep
            services.AddHttpClient<IViaCepProvider, ViaCepProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            // BrasilAPI
            services.AddHttpClient<IBrasilApiProvider, BrasilApiProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            return services;
        }
    }
}