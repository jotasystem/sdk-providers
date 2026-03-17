using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JotaSystem.Sdk.Providers.Storage.AzureBlob.Models;

namespace JotaSystem.Sdk.Providers.Storage.AzureBlob
{
    public class AzureBlobProvider(BlobServiceClient blobServiceClient) : IAzureBlobProvider
    {
        public async Task<AzureBlobResult> UploadAsync(Stream content, string container, string fileName, string? folder, string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException("Container inválido.", nameof(container));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("FileName inválido.", nameof(fileName));

            if (content.CanSeek)
                content.Position = 0;

            fileName = EnsureFileNameWithExtension(fileName, contentType);

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blobPath = string.IsNullOrWhiteSpace(folder)
                 ? fileName
                 : $"{folder.Trim('/')}/{fileName}";

            var blobClient = containerClient.GetBlobClient(blobPath);

            var headers = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(content, new BlobUploadOptions{ HttpHeaders = headers }, cancellationToken);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            return new AzureBlobResult
            {
                FileName = fileName,
                Path = $"{container}/{blobPath}",
                Url = blobClient.Uri.ToString(),
                ContentType = properties.Value.ContentType ?? "application/octet-stream",
                Size = properties.Value.ContentLength,
                Exists = true
            };
        }

        public async Task<AzureBlobResult> DownloadAsync(string path, CancellationToken cancellationToken = default)
        {
            var (container, blobPath) = ParsePath(path);

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobPath);

            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
                throw new FileNotFoundException($"Arquivo não encontrado: {path}");

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            return new AzureBlobResult
            {
                Content = response.Value.Content,
                FileName = Path.GetFileName(blobPath),
                Path = path,
                Url = blobClient.Uri.ToString(),
                ContentType = response.Value.Details.ContentType ?? "application/octet-stream",
                Size = response.Value.Details.ContentLength,
                Exists = true
            };
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var (container, blobPath) = ParsePath(path);

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobPath);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        public async Task<AzureBlobResult?> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            var (container, blobPath) = ParsePath(path);

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobPath);

            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
                return null;

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            return new AzureBlobResult
            {
                FileName = Path.GetFileName(blobPath),
                Path = path,
                Url = blobClient.Uri.ToString(),
                ContentType = properties.Value.ContentType ?? "application/octet-stream",
                Size = properties.Value.ContentLength,
                Exists = true
            };
        }

        public Task<string> GetUrlAsync(string path, CancellationToken cancellationToken = default)
        {
            var (container, blobPath) = ParsePath(path);

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobPath);

            return Task.FromResult(blobClient.Uri.ToString());
        }

        private static (string container, string blobPath) ParsePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path inválido.", nameof(path));

            var normalized = path.Trim('/');
            var index = normalized.IndexOf('/');

            if (index <= 0 || index == normalized.Length - 1)
                throw new InvalidOperationException($"Path inválido: {path}");

            var container = normalized[..index];
            var blobPath = normalized[(index + 1)..];

            return (container, blobPath);
        }

        private static string EnsureFileNameWithExtension(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(extension))
                return fileName;

            var mappedExtension = GetExtensionFromContentType(contentType);

            return string.IsNullOrWhiteSpace(mappedExtension)
                ? fileName
                : $"{fileName}{mappedExtension}";
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return string.Empty;

            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/svg+xml" => ".svg",
                "application/pdf" => ".pdf",
                "text/plain" => ".txt",
                "application/json" => ".json",
                "application/zip" => ".zip",
                _ => string.Empty
            };
        }
    }
}