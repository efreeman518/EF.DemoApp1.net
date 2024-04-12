using Azure.Identity;
using Domain.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Extensions;

namespace Test.Support;
public static class DbSupport
{
    //can only be registered once
    private static bool _keyStoreProviderRegistered = false;

    public static T ConfigureServicesTestDB<T>(ILogger logger, IServiceCollection services, string? dbConnectionString)
        where T : DbContext
    {
        //if _dbConnectionString is null, use the api defined DbContext/DB, otherwise switch out the DB here
        if (!string.IsNullOrEmpty(dbConnectionString))
        {
            logger.InfoLog($"Swapping services DbContext to use test database source: {dbConnectionString}");

            services.RemoveAll(typeof(DbContextOptions<T>));
            services.RemoveAll(typeof(T));
            services.AddDbContext<T>(options =>
            {
                if (dbConnectionString == "UseInMemoryDatabase")
                {
                    options.UseInMemoryDatabase($"Test.Endpoints-{Guid.NewGuid()}");
                }
                else
                {
                    options.UseSqlServer(dbConnectionString,
                        //retry strategy does not support user initiated transactions 
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        });

                    //SQL ALWAYS ENCRYPTED, the connection string must include "Column Encryption Setting=Enabled"
                    if (!_keyStoreProviderRegistered)
                    {
                        //sql always encrypted support; connection string must include "Column Encryption Setting=Enabled"
                        var credential = new DefaultAzureCredential();
                        SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);
                        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                        {
                            {
                                SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, sqlColumnEncryptionAzureKeyVaultProvider
                            }
                        });
                        _keyStoreProviderRegistered = true;
                    }
                }
            }, ServiceLifetime.Singleton);
        }

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<T>();

        //Environment.SetEnvironmentVariable("AKVCMKURL", "");
        //db.Database.Migrate(); //needs AKVCMKURL env var set
        db.Database.EnsureCreated(); //does not use migrations; uses DbContext to create tables

        return db;
    }

    /// <summary>
    /// Currently works only with existing database; not TestContainer or InMemoryDatabase
    /// </summary>
    /// <param name="snapshotName"></param>
    /// <param name="dbName"></param>
    /// <param name="dbConnectionString"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task CreateDbSnapshot(string snapshotName, string dbName, string dbConnectionString, CancellationToken cancellationToken = default)
    {
        var snapshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBSnapshot");
        var snapshotUtility = new SqlDatabaseSnapshotUtility(dbConnectionString);

        // Try to delete the snapshot in case it was left over from aborted test runs
        try { await snapshotUtility.DeleteSnapshotAsync(snapshotName, cancellationToken); }
        catch { /* expect fail when snapshot does not exist */ }

        await snapshotUtility.CreateSnapshotAsync(dbName, snapshotPath, snapshotName, cancellationToken);
    }

    /// <summary>
    /// Currently works only with existing database; not TestContainer or InMemoryDatabase
    /// </summary>
    /// <param name="snapshotName"></param>
    /// <param name="dbConnectionString"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task DeleteDbSnapshot(string snapshotName, string dbConnectionString, CancellationToken cancellationToken = default)
    {
        var snapshotUtility = new SqlDatabaseSnapshotUtility(dbConnectionString);
        await snapshotUtility.DeleteSnapshotAsync(snapshotName, cancellationToken);
    }

    private static readonly Random _R = new();
    public static TEnum? RandomEnumValue<TEnum>()
    {
        var v = Enum.GetValues(typeof(TEnum));
        return (TEnum?)v.GetValue(_R.Next(v.Length));
    }

    /// <summary>
    /// InMemory provider does not understand RowVersion like Sql EF Provider
    /// </summary>
    /// <param name="item"></param>
    public static void ApplyRowVersion(this TodoItem item)
    {
        item.RowVersion = GetRandomByteArray(16);
    }

    public static byte[] GetRandomByteArray(int sizeInBytes)
    {
        Random rnd = new();
        byte[] b = new byte[sizeInBytes]; // convert kb to byte
        rnd.NextBytes(b);
        return b;
    }
}
