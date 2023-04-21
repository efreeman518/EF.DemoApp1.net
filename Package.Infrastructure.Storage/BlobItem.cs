namespace Package.Infrastructure.Storage;
public class BlobItem
{
    public string Name { get; set; } = null!;

    public long Length { get; set; }

    public BlobType BlobItemType { get; set; }

    public IDictionary<string, string>? Metadata { get; set; }
}
