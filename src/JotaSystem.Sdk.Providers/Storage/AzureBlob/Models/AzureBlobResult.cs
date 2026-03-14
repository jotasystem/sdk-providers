namespace JotaSystem.Sdk.Providers.Storage.AzureBlob.Models
{
    public class AzureBlobResult
    {
        public string FileName { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string ContentType { get; set; } = "application/octet-stream";
        public long? Size { get; set; }
        public Stream? Content { get; set; }
        public bool Exists { get; set; }
    }
}