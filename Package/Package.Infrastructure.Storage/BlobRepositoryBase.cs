using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Package.Infrastructure.Storage;
public abstract class BlobRepositoryBase : IBlobRepository
{
    private readonly ILogger<BlobRepositoryBase> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    protected BlobRepositoryBase(ILogger<BlobRepositoryBase> logger, IAzureClientFactory<BlobServiceClient> clientFactory, string blobServiceClientName)
    {
        _logger = logger;
        _blobServiceClient = clientFactory.CreateClient(blobServiceClientName);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CreateContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        await _blobServiceClient.CreateBlobContainerAsync(containerInfo.ContainerName, (PublicAccessType)containerInfo.ContainerPublicAccessType, null, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeleteContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        await _blobServiceClient.DeleteBlobContainerAsync(containerInfo.ContainerName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<BlobItem>> ListContainerBlobsAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = await GetBlobContainerClientAsync(containerInfo, cancellationToken);
        List<BlobItem> items = new();

        await foreach (Azure.Storage.Blobs.Models.BlobItem blobItem in container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            items.Add(new BlobItem
            {
                Name = blobItem.Name,
                BlobType = (BlobType)(blobItem.Properties.BlobType ?? Azure.Storage.Blobs.Models.BlobType.Block),
                Length = blobItem.Properties.ContentLength ?? 0,
                Metadata = blobItem.Metadata
            });
        }

        return items;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="blobName"></param>
    /// <param name="stream"></param>
    /// <param name="contentType"></param>
    /// <param name="encrypt"></param>
    /// <param name="metadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task UploadBlobStreamAsync(ContainerInfo containerInfo, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerInfo, cancellationToken);
        await UploadContainerBlob(containerClient, blobName, stream, contentType, encrypt, metadata, cancellationToken);
    }

    public async Task UploadBlobStreamToUriAsync(Uri sasUri, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = new(sasUri);
        await UploadContainerBlob(containerClient, blobName, stream, contentType, encrypt, metadata, cancellationToken);
    }

    private async Task UploadContainerBlob(BlobContainerClient containerClient, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        _ = encrypt; //remove compiler message Remove unused parameter (IDE0060)

        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        BlobUploadOptions options = new();

        if (contentType != null)
        {
            BlobHttpHeaders blobHttpHeaders = new()
            {
                ContentType = contentType
            };
            options.HttpHeaders = blobHttpHeaders;
        }
        if (metadata != null) options.Metadata = metadata;

        _logger.LogInformation("UploadContainerBlob Start - {Container} {Blob}", containerClient.Name, blobName);
        await blobClient.UploadAsync(stream, options, cancellationToken);
        _logger.LogInformation("UploadContainerBlob Finish - {Container} {Blob}", containerClient.Name, blobName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="blobName"></param>
    /// <param name="decrypt"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Stream> DownloadBlobStreamAsync(ContainerInfo containerInfo, string blobName, bool decrypt = false, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerInfo, cancellationToken);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        BlobOpenReadOptions options = new(false);

        _logger.LogInformation("DownloadBlobStreamAsync Starting - {Container} {Blob}", containerInfo.ContainerName, blobName);
        return await blobClient.OpenReadAsync(options, cancellationToken);

        ///var download = await blobClient.DownloadAsync(cancellationToken);
        ///return download.Value.Content;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="blobName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeleteBlobAsync(ContainerInfo containerInfo, string blobName, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerInfo, cancellationToken);
        BlobClient blob = containerClient.GetBlobClient(blobName);

        _logger.LogInformation("DeleteBlobAsync Start - {Container} {Blob}", containerInfo.ContainerName, blobName);
        await blob.DeleteAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("DeleteBlobAsync Finish - {Container} {Blob}", containerInfo.ContainerName, blobName);
    }

    private async Task<BlobContainerClient> GetBlobContainerClientAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerInfo.ContainerName);

        if (!(await container.ExistsAsync(cancellationToken)))
        {
            if (containerInfo.CreateContainerIfNotExist)
            {
                _logger.Log(LogLevel.Information, $"GetBlobContainerClientAsync - Storage Account Container '{containerInfo.ContainerName}' does not exist; attempting to create.");
                await container.CreateIfNotExistsAsync((PublicAccessType)containerInfo.ContainerPublicAccessType, cancellationToken: cancellationToken);
                _logger.Log(LogLevel.Information, $"GetBlobContainerClientAsync - Storage Account Container '{containerInfo.ContainerName}' created.");
            }
            else
                throw new InvalidOperationException($"Azure Storage Container does not exist and createifNotexist = false.");
        }

        return container;
    }
}
