namespace Package.Infrastructure.Storage;

public class ContainerInfo
{
    public string ContainerName { get; set; } = null!;

    public bool CreateContainerIfNotExist { get; set; }

    public ContainerPublicAccessType ContainerPublicAccessType { get; set; }
}
