namespace JotaSystem.Sdk.Providers.Storage.AzureBlob
{
    public interface IAzureBlobProvider
    {
        Task<Uri> GetUploadUrlAsync(string key, string contentType, TimeSpan expiresIn);

        Task<Uri> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? fileName = null);

        Task UploadAsync(string key, Stream content, string contentType);

        Task DeleteAsync(string key);

        Task MoveAsync(string sourceKey, string destinationKey, bool overwrite = false);
    }
}