namespace Package.Infrastructure.Cache;
public static class RedisConfigurationUtility
{
    public static RedisConfiguration ParseRedisConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        var config = new RedisConfiguration();
        var parts = connectionString.Split(',');

        foreach (var part in parts)
        {
            if (part.Contains('='))
            {
                var kvp = part.Split('=', 2);
                if (kvp.Length == 2)
                {
                    var key = kvp[0].Trim().ToLowerInvariant();
                    var value = kvp[1].Trim();

                    switch (key)
                    {
                        case "password":
                            config.Password = value;
                            break;
                        case "port":
                            if (int.TryParse(value, out int portNum))
                                config.Port = portNum;
                            break;
                            // Add other parameters as needed
                    }
                }
            }
            else
            {
                // Handle endpoint:port format
                var hostParts = part.Split(':', 2);
                config.EndpointUrl = hostParts[0].Trim();

                // If port is specified in the endpoint part
                if (hostParts.Length > 1 && int.TryParse(hostParts[1], out int port))
                {
                    config.Port = port;
                }
            }
        }

        // Set default port if not specified
        if (config.Port == 0)
        {
            config.Port = 6379; // Default Redis port
        }

        return config;
    }
}
