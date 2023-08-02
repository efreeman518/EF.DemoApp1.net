using Azure.Storage.Blobs.Models;

namespace Package.Infrastructure.Storage;

public interface IBlobRepository
{
    Task CreateContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default);

    Task DeleteContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BlobItem>, string?)> QueryPageBlobsAsync(ContainerInfo containerInfo, string? continuationToken = null,
        BlobTraits blobTraits = BlobTraits.None, BlobStates blobStates = BlobStates.None, string? prefix = null, CancellationToken cancellationToken = default);

    Task<IAsyncEnumerable<BlobItem>> GetStreamBlobList(ContainerInfo containerInfo,
        BlobTraits blobTraits = BlobTraits.None, BlobStates blobStates = BlobStates.None, string? prefix = null, CancellationToken cancellationToken = default);

    Task UploadBlobStreamAsync(ContainerInfo containerInfo, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    Task UploadBlobStreamToUriAsync(Uri sasUri, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    Task<Stream> DownloadBlobStreamAsync(ContainerInfo containerInfo, string blobName, bool decrypt = false, CancellationToken cancellationToken = default);

    Task DeleteBlobAsync(ContainerInfo containerInfo, string blobName, CancellationToken cancellationToken = default);
}
