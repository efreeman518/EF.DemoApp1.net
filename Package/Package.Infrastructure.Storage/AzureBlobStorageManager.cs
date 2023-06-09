using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Package.Infrastructure.Storage;

//https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet
public class AzureBlobStorageManager : IAzureBlobStorageManager
{
    private readonly ILogger<AzureBlobStorageManager> _logger;
    private readonly ConcurrentDictionary<string, BlobServiceClient> _blobServiceClients = new();
    private static readonly object _lock = new();

    public AzureBlobStorageManager(ILogger<AzureBlobStorageManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task CreateContainerAsync(BlobStorageRequest request, CancellationToken cancellationToken = default)
    {
        BlobServiceClient blobServiceClient = GetBlobServiceClient(request);
        await blobServiceClient.CreateBlobContainerAsync(request.ContainerName, (PublicAccessType)request.ContainerPublicAccessType, null, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeleteContainerAsync(BlobStorageRequest request, CancellationToken cancellationToken = default)
    {
        BlobServiceClient blobServiceClient = GetBlobServiceClient(request);
        await blobServiceClient.DeleteBlobContainerAsync(request.ContainerName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<BlobItem>> ListContainerBlobsAsync(BlobStorageRequest request, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = await GetBlobContainerClientAsync(request, cancellationToken);
        List<BlobItem> items = new();

        await foreach (Azure.Storage.Blobs.Models.BlobItem blobItem in container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            items.Add(new BlobItem
            {
                Name = blobItem.Name,
                BlobItemType = (BlobType)(blobItem.Properties.BlobType ?? Azure.Storage.Blobs.Models.BlobType.Block),
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
    public async Task UploadBlobStreamAsync(BlobStorageRequest request, string blobName, Stream stream, string? contentType = null, bool encrypt = false, IDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(request, cancellationToken);
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
    public async Task<Stream> DownloadBlobStreamAsync(BlobStorageRequest request, string blobName, bool decrypt = false, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(request, cancellationToken);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        BlobOpenReadOptions options = new(false);

        _logger.LogInformation("DownloadBlobStreamAsync Starting - {Container} {Blob}", request.ContainerName, blobName);
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
    public async Task DeleteBlobAsync(BlobStorageRequest request, string blobName, CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = await GetBlobContainerClientAsync(request, cancellationToken);
        BlobClient blob = containerClient.GetBlobClient(blobName);

        _logger.LogInformation("DeleteBlobAsync Start - {Container} {Blob}", request.ContainerName, blobName);
        await blob.DeleteAsync(cancellationToken: cancellationToken);
        _logger.LogInformation("DeleteBlobAsync Finish - {Container} {Blob}", request.ContainerName, blobName);
    }

    private BlobServiceClient GetBlobServiceClient(BlobStorageRequest request)
    {
        //hash the service name
        string key = CreateMD5Hash($"{request.StorageAccountUrl ?? ""}{request.ConnectionString ?? ""}");

        //check for the BlobServiceClient
        if (_blobServiceClients.TryGetValue(key, out var blobServiceClient1)) return blobServiceClient1;

        //create and cache
        lock (_lock)
        {
            // Try to fetch from cache again now that we have entered the critical section
            if (_blobServiceClients.TryGetValue(key, out var blobServiceClient)) return blobServiceClient;

            if (request.StorageAccountUrl == null && request.ConnectionString == null)
                throw new InvalidOperationException($"Azure Storage Request Container {request.ContainerName} needs either StorageAccountQueueUrl or ConnectionString. Both are null.");

            //create, update cache, and return.
            //Acquired tokens are cached by the credential instance. Token lifetime and refreshing is handled automatically.
            //https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet#methods
            blobServiceClient = (request.StorageAccountUrl != null)
            ? new BlobServiceClient(new Uri(request.StorageAccountUrl), new DefaultAzureCredential(), null)
            : new BlobServiceClient(request.ConnectionString);

            _blobServiceClients.TryAdd(key, blobServiceClient);

            return blobServiceClient;
        }
    }

    private async Task<BlobContainerClient> GetBlobContainerClientAsync(BlobStorageRequest request, CancellationToken cancellationToken = default)
    {
        //hash the service name
        var blobServiceClient = GetBlobServiceClient(request);

        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(request.ContainerName);

        if (!(await container.ExistsAsync(cancellationToken)))
        {
            if (request.CreateContainerIfNotExist)
            {
                _logger.Log(LogLevel.Information, $"GetBlobContainerClientAsync - Storage Account Container '{request.ContainerName}' does not exist; attempting to create.");
                await container.CreateIfNotExistsAsync((PublicAccessType)request.ContainerPublicAccessType, cancellationToken: cancellationToken);
                _logger.Log(LogLevel.Information, $"GetBlobContainerClientAsync - Storage Account Container '{request.ContainerName}' created.");
            }
            else
                throw new InvalidOperationException($"Azure Storage Container does not exist and createifNotexist = false.");
        }

        return container;
    }

    private static string CreateMD5Hash(string input)
    {
        // Step 1, calculate MD5 hash from input
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);

        // Step 2, convert byte array to hex string
        StringBuilder sb = new();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
