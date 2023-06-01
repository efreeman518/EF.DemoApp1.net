namespace Package.Infrastructure.Storage;
public class AzureBlobStorageManagerSettings
{
    public const string ConfigSectionName = "AzureBlobStorageManagerSettings";
    public int RefreshIntervalSeconds { get; set; } = 550;
}

