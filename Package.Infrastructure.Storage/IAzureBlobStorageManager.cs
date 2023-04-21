namespace Package.Infrastructure.Storage;

public interface IAzureBlobStorageManager
{
    Task CreateContainerAsync(BlobStorageRequest request, CancellationToken cancellationToken = default);

    Task DeleteContainerAsync(BlobStorageRequest request, CancellationToken cancellationToken = default);

    Task<List<BlobItem>> ListContainerBlobsAsync(BlobStorageRequest request, CancellationToken cancellationToken = default);

    Task UploadBlobStreamAsync(BlobStorageRequest request, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    Task UploadBlobStreamToUriAsync(Uri sasUri, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    Task<Stream> DownloadBlobStreamAsync(BlobStorageRequest request, string blobName, bool decrypt = false, CancellationToken cancellationToken = default);

    Task DeleteBlobAsync(BlobStorageRequest request, string blobName, CancellationToken cancellationToken = default);
}
