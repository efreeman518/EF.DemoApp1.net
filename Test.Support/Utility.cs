using Microsoft.Extensions.Configuration;

namespace Test.Support;
public static class Utility
{
    public static IConfigurationBuilder BuildConfiguration(string? path = "appsettings.json", bool includeEnvironmentVars = true)
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        if (path != null) builder.AddJsonFile(path);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();
        return builder;
    }
}
