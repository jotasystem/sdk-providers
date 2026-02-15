using JotaSystem.Sdk.Providers.Storage.AzureBlob;
using Microsoft.Extensions.DependencyInjection;

namespace JotaSystem.Sdk.Providers.Storage
{
    public static class StorageExtensions
    {
        public static JotaSystemSdkBuilder AddAzureBlob(this JotaSystemSdkBuilder builder, Action<AzureBlobOptions> configure)
        {
            var options = new AzureBlobOptions();
            configure(options);

            builder.Services.AddSingleton<IAzureBlobProvider>(new AzureBlobProvider(options));
            return builder;
        }
    }
}
