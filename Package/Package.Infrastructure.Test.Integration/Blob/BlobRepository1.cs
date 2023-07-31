using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Storage;

namespace Package.Infrastructure.Test.Integration.Blob;

/// <summary>
/// Implementation for each BlobServiceClientName
/// </summary>
internal class BlobRepository1 : BlobRepositoryBase, IBlobRepository1
{
    public BlobRepository1(ILogger<BlobRepository1> logger, IAzureClientFactory<BlobServiceClient> clientFactory, IOptions<BlobRepositorySettings1> settings)
        : base(logger, clientFactory, settings.Value.BlobServiceClientName)
    {
    }
}
