namespace Package.Infrastructure.Storage;
public class BlobStorageRequest
{
    //public string? StorageAccountUrl { get; set; }

    //public string? ConnectionString { get; set; }

    /// <summary>
    /// configured at startup with Creds/StorageAccountUri/ConnectionString
    /// </summary>
    public string ClientName { get; set; } = null!;

    public string ContainerName { get; set; } = null!;

    public bool CreateContainerIfNotExist { get; set; }

    public ContainerPublicAccessType ContainerPublicAccessType { get; set; }
}
