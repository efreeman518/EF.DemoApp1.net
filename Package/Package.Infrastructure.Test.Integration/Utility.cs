using Microsoft.Extensions.Configuration;

namespace Package.Infrastructure.Test.Integration;

internal static class Utility
{
    public static IConfigurationRoot BuildConfiguration<T>() where T : class
    {
        //var devEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        //var isDevelopment = devEnvironmentVariable?.ToLower() == "development";

        var builder = BuildConfigurationBuilder();
        builder.AddUserSecrets<T>();
        IConfigurationRoot config = builder.Build();
        return config;
    }

    private static IConfigurationBuilder BuildConfigurationBuilder(string? path = "appsettings.json", bool includeEnvironmentVars = true)
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        if (path != null) builder.AddJsonFile(path);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();
        return builder;
    }

}
