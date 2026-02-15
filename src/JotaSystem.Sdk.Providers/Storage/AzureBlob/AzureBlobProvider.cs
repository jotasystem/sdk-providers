using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace JotaSystem.Sdk.Providers.Storage.AzureBlob
{
    public class AzureBlobProvider(AzureBlobOptions options) : IAzureBlobProvider
    {
        private readonly AzureBlobOptions _options = options;

        private readonly BlobContainerClient _container =
            new(options.ConnectionString, options.ContainerName);

        public async Task UploadAsync(string key, Stream content, string contentType)
        {
            var blob = _container.GetBlobClient(key);

            await blob.UploadAsync(content, new BlobHttpHeaders
            {
                ContentType = contentType
            });
        }

        public Task DeleteAsync(string key)
        {
            var blob = _container.GetBlobClient(key);
            return blob.DeleteIfExistsAsync();
        }

        public async Task MoveAsync(string sourceKey, string destinationKey, bool overwrite = false)
        {
            var source = _container.GetBlobClient(sourceKey);
            var dest = _container.GetBlobClient(destinationKey);

            if (!await source.ExistsAsync())
                throw new FileNotFoundException("Arquivo de origem não encontrado.", sourceKey);

            if (!overwrite && await dest.ExistsAsync())
                throw new InvalidOperationException("Arquivo de destino já existe.");

            await dest.StartCopyFromUriAsync(source.Uri);
            await source.DeleteAsync();
        }

        public Task<Uri> GetUploadUrlAsync(string key, string contentType, TimeSpan expiresIn)
        {
            var blob = _container.GetBlobClient(key);

            var sas = new BlobSasBuilder
            {
                BlobContainerName = _container.Name,
                BlobName = key,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn),
                ContentType = contentType
            };

            sas.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            return Task.FromResult(blob.GenerateSasUri(sas));
        }

        public Task<Uri> GetDownloadUrlAsync(string key, TimeSpan expiresIn, string? fileName = null)
        {
            var blob = _container.GetBlobClient(key);

            var sas = new BlobSasBuilder
            {
                BlobContainerName = _container.Name,
                BlobName = key,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
            };

            sas.SetPermissions(BlobSasPermissions.Read);

            if (!string.IsNullOrWhiteSpace(fileName))
                sas.ContentDisposition = $"attachment; filename=\"{fileName}\"";

            return Task.FromResult(blob.GenerateSasUri(sas));
        }
    }
}