namespace JotaSystem.Sdk.Providers.Storage.AzureBlob
{
    public class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = default!;
        public string ContainerName { get; set; } = default!;
    }
}