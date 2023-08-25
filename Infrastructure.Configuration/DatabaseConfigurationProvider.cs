using Domain.Model;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Configuration;

/// <summary>
/// https://mousavi310.github.io/posts/a-refreshable-sql-server-configuration-provider-for-net-core/
/// </summary>
public class DatabaseConfigurationProvider : ConfigurationProvider
{
    private readonly DatabaseConfigurationSource _source;
    private readonly Action<DbContextOptionsBuilder> _optionsAction;
    

    public DatabaseConfigurationProvider(DatabaseConfigurationSource source, Action<DbContextOptionsBuilder> optionsAction)
    {
        _source = source;
        _optionsAction = optionsAction;

        // Load the configuration data from the database
        //Load();
        // Register a callback to reload the configuration data when the change token changes
        //ChangeToken.OnChange(
        //    () => _source.GetReloadToken(),
        //    () => Load());
    }

    public override void Load()
    {
        _ = _source.GetHashCode();
        Data.Clear();
        // Read data from database and populate Data dictionary
        var builder = new DbContextOptionsBuilder<SystemSettingsDbContext>();
        _optionsAction(builder);
        using var dbContext = new SystemSettingsDbContext(builder.Options);
        dbContext.Database.EnsureCreated();
        Data = !dbContext.SystemSettings.Any()
            ? CreateAndSaveDefaultValues(dbContext)
            : dbContext.SystemSettings.ToDictionary(c => c.Key, c => c.Value);
    }

    private static IDictionary<string, string?> CreateAndSaveDefaultValues(SystemSettingsDbContext context)
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
