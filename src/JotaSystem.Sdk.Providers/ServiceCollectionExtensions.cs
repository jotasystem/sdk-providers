using JotaSystem.Sdk.Providers.Services.ViaCep;
using JotaSystem.Sdk.Providers.Services.ViaCep.Interfaces;
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
            // Registra ViaCepProvider com HttpClient
            services.AddHttpClient<IViaCepProvider, ViaCepProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            return services;
        }
    }
}