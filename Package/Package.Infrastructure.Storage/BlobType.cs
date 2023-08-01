namespace Package.Infrastructure.Storage;

/// <summary>
/// Maps to Azure.Storage.Blobs.Models so client does not need a reference to Azure SDK
/// </summary>
public enum BlobType
{
    Block,
    Page,
    Append
}
