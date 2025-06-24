namespace Package.Infrastructure.Cache;

public class RedisConfiguration
{
    public string EndpointUrl { get; set; } = null!;
    public int Port { get; set; }
    public string? Password { get; set; }
}
