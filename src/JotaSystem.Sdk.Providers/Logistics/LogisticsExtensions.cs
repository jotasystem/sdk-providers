using JotaSystem.Sdk.Providers.Logistics.Address.BrasilApi;
using JotaSystem.Sdk.Providers.Logistics.Address.OpenCep;
using JotaSystem.Sdk.Providers.Logistics.Address.ViaCep;
using JotaSystem.Sdk.Providers.Logistics.Shipping.Correios;
using Microsoft.Extensions.DependencyInjection;
using ViaCep;

namespace JotaSystem.Sdk.Providers.Logistics
{
    public static class LogisticsExtensions
    {
        public static JotaSystemSdkBuilder AddViaCep(this JotaSystemSdkBuilder builder)
        {
            builder.Services.AddHttpClient<IViaCepClient, ViaCepClient>(client =>
            {
                client.BaseAddress = new Uri("https://viacep.com.br/");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });
            builder.Services.AddTransient<IViaCepProvider, ViaCepProvider>();

            return builder;
        }

        public static JotaSystemSdkBuilder AddOpenCep(this JotaSystemSdkBuilder builder)
        {
            builder.Services.AddHttpClient<IOpenCepProvider, OpenCepProvider>(client =>
            {
                client.BaseAddress = new Uri("https://opencep.com/");
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

        public static JotaSystemSdkBuilder AddCorreios(this JotaSystemSdkBuilder builder, Action<CorreiosOptions>? configure = null)
        {
            var options = new CorreiosOptions();
            configure?.Invoke(options);

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<ICorreiosTokenCache, CorreiosTokenCache>();

            builder.Services.AddHttpClient(CorreiosHttpClientNames.Token, client =>
            {
                client.BaseAddress = new Uri(options.TokenBaseUrl);
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            builder.Services.AddHttpClient(CorreiosHttpClientNames.Price, client =>
            {
                client.BaseAddress = new Uri(options.PriceBaseUrl);
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.Add("User-Agent", "JotaSystemSdk/1.0");
            });

            builder.Services.AddScoped<ICorreiosProvider, CorreiosProvider>();
            return builder;
        }
    }
}