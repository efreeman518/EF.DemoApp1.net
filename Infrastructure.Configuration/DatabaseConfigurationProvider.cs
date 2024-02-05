using Domain.Model;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Configuration;

/// <summary>
/// https://mousavi310.github.io/posts/a-refreshable-sql-server-configuration-provider-for-net-core/
/// https://www.c-sharpcorner.com/article/using-change-tokens-in-net-7/
/// </summary>
public class DatabaseConfigurationProvider : ConfigurationProvider
{
    private readonly DatabaseConfigurationSource _source;
    private readonly Action<DbContextOptionsBuilder> _optionsAction;

    //private bool _hasChanged;

    //public bool HasChanged => _hasChanged;
    //public bool ActiveChangeCallbacks => true;

    public DatabaseConfigurationProvider(DatabaseConfigurationSource source, Action<DbContextOptionsBuilder> optionsAction)
    {
        _source = source;
        _optionsAction = optionsAction;

        if (_source.DbConfigRefresher != null)
        {
            ChangeToken.OnChange(
                () => _source.DbConfigRefresher.StartWatch(),
                Load
            );
        }
    }

    public override void Load()
    {
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

    private static Dictionary<string, string?> CreateAndSaveDefaultValues(SystemSettingsDbContext context)
    {
        var settings = new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Parent:SettingName1"] = "Value1",
            ["Parent:SettingName2"] = "Value2",
            ["Parent:SettingName3"] = "Value3"
        };

        context.SystemSettings.AddRange(
            settings.Select(kvp => new SystemSetting(kvp.Key, kvp.Value) { CreatedBy = "Initializer" })
                    .ToArray());

        context.SaveChanges();

        return settings;
    }

}
