using JotaSystem.Sdk.Providers.Storage.AzureBlob.Models;

namespace JotaSystem.Sdk.Providers.Storage.AzureBlob
{
    public interface IAzureBlobProvider
    {
        Task<AzureBlobResult> UploadAsync(Stream content, string container, string fileName, string? folder, string contentType = "application/octet-stream", CancellationToken cancellationToken = default);
        Task<AzureBlobResult> DownloadAsync(string path, CancellationToken cancellationToken = default);
        Task DeleteAsync(string path, CancellationToken cancellationToken = default);
        Task<AzureBlobResult?> GetAsync(string path, CancellationToken cancellationToken = default);
        Task<string> GetUrlAsync(string path, CancellationToken cancellationToken = default);
    }
}