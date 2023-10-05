using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Package.Infrastructure.Storage;

namespace Package.Infrastructure.Test.Integration.Blob;

/// <summary>
/// Implementation for each BlobServiceClientName
/// </summary>
internal class BlobRepository1(ILogger<BlobRepository1> logger, IOptions<BlobRepositorySettings1> settings, 
    IAzureClientFactory<BlobServiceClient> clientFactory) : BlobRepositoryBase(logger, settings, clientFactory), IBlobRepository1
{
}
