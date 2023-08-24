using Domain.Model;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace SampleApp.Bootstrapper.Configuration;

/// <summary>
/// Custom Configuration provider getting settings from a database table
/// https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider
/// </summary>
public class EntityConfigurationProvider : ConfigurationProvider
{
    private readonly string? _connectionString;

    public EntityConfigurationProvider(string? connectionString) => _connectionString = connectionString;

    public override void Load()
    {
        var options = new DbContextOptionsBuilder<SystemSettingsDbContext>();
        if (string.IsNullOrEmpty(_connectionString) || _connectionString == "UseInMemoryDatabase")
        {
            options.UseInMemoryDatabase("SystemSettingsContext");
        }
        else
        {
            options.UseSqlServer(_connectionString);
        }

        using var dbContext = new SystemSettingsDbContext(options.Options);

        dbContext.Database.EnsureCreated();

        Data = dbContext.SystemSettings.Any()
            ? dbContext.SystemSettings.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase)
            : CreateAndSaveDefaultValues(dbContext);
    }

    static IDictionary<string, string?> CreateAndSaveDefaultValues(SystemSettingsDbContext context)
    {
        var settings = new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Parent:SettingName1"] = "Value1",
            ["Parent:SettingName2"] = "Value2",
            ["Parent:SettingName3"] = "Value3"
        };

        context.SystemSettings.AddRange(
            settings.Select(kvp => new SystemSetting(kvp.Key, kvp.Value))
                    .ToArray());

        context.SaveChanges();

        return settings;
    }
}
