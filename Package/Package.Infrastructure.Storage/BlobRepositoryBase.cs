using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Common.Extensions;

namespace Package.Infrastructure.Storage;

/// <summary>
/// Client will need a reference to Azure.Storage.Blobs.Models as there are too many models not worth maintaining a mapping for insulation
/// </summary>
public abstract class BlobRepositoryBase : IBlobRepository
{
    private readonly ILogger<BlobRepositoryBase> _logger;
    private readonly BlobRepositorySettingsBase _settings;
    private readonly BlobServiceClient _blobServiceClient;

    protected BlobRepositoryBase(ILogger<BlobRepositoryBase> logger, IOptions<BlobRepositorySettingsBase> settings, IAzureClientFactory<BlobServiceClient> clientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _blobServiceClient = clientFactory.CreateClient(_settings.BlobServiceClientName);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CreateContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        _ = _settings.GetHashCode(); //remove compiler warning
        await _blobServiceClient.CreateBlobContainerAsync(containerInfo.ContainerName, (PublicAccessType)containerInfo.ContainerPublicAccessType,
            null, cancellationToken: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeleteContainerAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        await _blobServiceClient.DeleteBlobContainerAsync(containerInfo.ContainerName, cancellationToken: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// List container blobs
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(IReadOnlyList<BlobItem>, string?)> QueryPageBlobsAsync(ContainerInfo containerInfo, string? continuationToken = null,
        BlobTraits blobTraits = BlobTraits.None, BlobStates blobStates = BlobStates.None, string? prefix = null, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = await GetBlobContainerClientAsync(containerInfo, cancellationToken);
        var pageable = container.GetBlobsAsync(blobTraits, blobStates, prefix, cancellationToken);

        (var blobPage, continuationToken) = await pageable.GetPageAsync(continuationToken, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return (blobPage, continuationToken);
    }

    public async Task<IAsyncEnumerable<BlobItem>> GetStreamBlobList(ContainerInfo containerInfo,
        BlobTraits blobTraits = BlobTraits.None, BlobStates blobStates = BlobStates.None, string? prefix = null, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = await GetBlobContainerClientAsync(containerInfo, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        return container.GetBlobsAsync(blobTraits, blobStates, prefix, cancellationToken);
    }

    /// <summary>
    /// Generate a limited access/limitid time uri for a blob; generally provided to a client to upload/download a blob
    /// </summary>
    /// <param name="containerInfo"></param>
    /// <param name="blobName"></param>
    /// <param name="permissions"></param>
    /// <param name="expiresOn"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<Uri?> GenerateBlobSasUriAsync(ContainerInfo containerInfo, string blobName, BlobSasPermissions permissions,
         DateTimeOffset expiresOn, SasIPRange? ipRange = null, CancellationToken cancellationToken = default)
    {
        //if managed identities are used, https://learn.microsoft.com/en-us/rest/api/storageservices/create-user-delegation-sas
        //_blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, expiresOn, cancellationToken: cancellationToken);

        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerInfo, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        if (blobClient.CanGenerateSasUri)
        {
            // Create a SAS token 
            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = containerInfo.ContainerName,
                BlobName = blobName,
                Resource = "b",  //blob
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = expiresOn,
            };
            if (ipRange != null) sasBuilder.IPRange = (SasIPRange)ipRange;
            sasBuilder.SetPermissions(permissions);

            return blobClient.GenerateSasUri(sasBuilder);
        }
        else
        {
            throw new InvalidOperationException($"BlobClient not authorized to generate sas; Client must be authenticated with Azure.Storage.StorageSharedKeyCredential.");
        }
    }

    public async Task UploadBlobStreamAsync(Uri sasUri, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = new(sasUri);
        await UploadBlobStream(blobClient, stream, contentType, encrypt, metadata, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
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
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        await UploadBlobStream(blobClient, stream, contentType, encrypt, metadata, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    private async Task UploadBlobStream(BlobClient blobClient, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        _ = encrypt; //remove compiler message Remove unused parameter (IDE0060)

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

        _logger.LogInformation("UploadBlob Start - {Container} {Blob}", blobClient.BlobContainerName, blobClient.Name);
        await blobClient.UploadAsync(stream, options, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        _logger.LogInformation("UploadBlob Finish - {Container} {Blob}", blobClient.BlobContainerName, blobClient.Name);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerInfo"></param>
    /// <param name="blobName"></param>
    /// <param name="decrypt"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Stream> StartDownloadBlobStreamAsync(ContainerInfo containerInfo, string blobName, bool decrypt = false, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerInfo, cancellationToken);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return await StartDownloadBlobStreamAsync(blobClient, decrypt, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);

        ///var download = await blobClient.DownloadAsync(cancellationToken);
        ///return download.Value.Content;
    }

    public async Task<Stream> StartDownloadBlobStreamAsync(Uri sasUri, bool decrypt = false, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = new(sasUri);
        return await StartDownloadBlobStreamAsync(blobClient, decrypt, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    private async Task<Stream> StartDownloadBlobStreamAsync(BlobClient blobClient, bool decrypt = false, CancellationToken cancellationToken = default)
    {
        _ = decrypt; //remove compiler message Remove unused parameter (IDE0060)
        BlobOpenReadOptions options = new(false);
        _logger.LogInformation("BlobStartDownloadStreamAsync - {Container} {Blob}", blobClient.BlobContainerName, blobClient.Name);
        return await blobClient.OpenReadAsync(options, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task DeleteBlobAsync(ContainerInfo containerInfo, string blobName, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(containerInfo, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        BlobClient blob = containerClient.GetBlobClient(blobName);
        await DeleteBlobAsync(blob, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task DeleteBlobAsync(Uri sasUri, CancellationToken cancellationToken = default)
    {
        BlobClient blobClient = new(sasUri);
        await DeleteBlobAsync(blobClient, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="blobName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task DeleteBlobAsync(BlobClient blobClient, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteBlobAsync Start - {Container} {Blob}", blobClient.BlobContainerName, blobClient.Name);
        await blobClient.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
        _logger.LogInformation("DeleteBlobAsync Finish - {Container} {Blob}", blobClient.BlobContainerName, blobClient.Name);
    }

    private async Task<BlobContainerClient> GetBlobContainerClientAsync(ContainerInfo containerInfo, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = _blobServiceClient.GetBlobContainerClient(containerInfo.ContainerName);

        if (!(await container.ExistsAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None)))
        {
            if (containerInfo.CreateContainerIfNotExist)
            {
                _logger.Log(LogLevel.Information, $"GetBlobContainerClientAsync - Storage Account Container '{containerInfo.ContainerName}' does not exist; attempting to create.");
                await container.CreateIfNotExistsAsync((PublicAccessType)containerInfo.ContainerPublicAccessType, cancellationToken: cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None);
                _logger.Log(LogLevel.Information, $"GetBlobContainerClientAsync - Storage Account Container '{containerInfo.ContainerName}' created.");
            }
            else
                throw new InvalidOperationException($"Azure Storage Container does not exist and createifNotexist = false.");
        }

        return container;
    }
}
