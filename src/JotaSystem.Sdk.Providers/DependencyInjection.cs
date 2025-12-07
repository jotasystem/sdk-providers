using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers
{
    public class JotaSystemSdkBuilder(IServiceCollection services)
    {
        public IServiceCollection Services { get; } = services;
    }

    public static class DependencyInjection
    {
        /// <summary>
        /// Registra o SDK da JotaSystem e retorna um builder para adicionar providers.
        /// </summary>
        public static JotaSystemSdkBuilder AddJotaSystemProviders(this IServiceCollection services)
        {
            return new JotaSystemSdkBuilder(services);
        }
    }
}