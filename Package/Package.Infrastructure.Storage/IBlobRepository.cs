namespace Package.Infrastructure.Storage;
public interface IBlobRepository
{
    Task CreateContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default);

    Task DeleteContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default);

    Task<List<BlobItem>> ListContainerBlobsAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default);

    Task UploadBlobStreamAsync(ContainerInfo containerInfo, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    Task UploadBlobStreamToUriAsync(Uri sasUri, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);

    Task<Stream> DownloadBlobStreamAsync(ContainerInfo containerInfo, string blobName, bool decrypt = false, CancellationToken cancellationToken = default);

    Task DeleteBlobAsync(ContainerInfo containerInfo, string blobName, CancellationToken cancellationToken = default);
}
