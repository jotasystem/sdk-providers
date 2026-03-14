using Azure.Storage.Blobs;
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

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new InvalidOperationException("AzureBlobOptions.ConnectionString não foi configurada.");

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton(_ => new BlobServiceClient(options.ConnectionString));
            builder.Services.AddScoped<IAzureBlobProvider, AzureBlobProvider>();

            return builder;
        }
    }
}