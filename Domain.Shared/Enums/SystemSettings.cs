namespace Domain.Shared.Enums;

[Flags]
public enum SystemSettings
{
    Configuration = 1,
    MemoryCache = 2,
    DistributedCache = 4
}
