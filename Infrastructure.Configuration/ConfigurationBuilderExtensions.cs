using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;
public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddDatabaseSource(this IConfigurationBuilder builder, string connectionString)
    {
        var source = new DatabaseConfigurationSource(options =>
        {
            if (string.IsNullOrEmpty(connectionString) || connectionString == "UseInMemoryDatabase")
            {
                options.UseInMemoryDatabase("SystemSettingsContext");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        }); 
        return builder.Add(source);
    }
}
