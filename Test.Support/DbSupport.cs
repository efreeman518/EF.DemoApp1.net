using Azure.Identity;
using Domain.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Extensions;

namespace Test.Support;

public static class DbSupport
{
    //can only be registered once; web application factory may have already registered
    private static bool _keyStoreProviderRegistered = false;

    /// <summary>
    /// Swaps out the DbContexts based on connection string for testing
    /// Note: This method does not ensure the DB is created; make sure to run migrations or dbContext.EnsureCreatedAsync() before using the DB
    /// Note: current .net9 service collection does not remove with RemoveAll<T>, so this doesn't work unless skipping the main service DbContext registration 
    /// </summary>
    /// <typeparam name="TTrxn"></typeparam>
    /// <typeparam name="TQuery"></typeparam>
    /// <param name="services"></param>
    /// <param name="dbConnectionString"></param>
    /// <param name="dbName">Used to name in-memory dbContext</param>
    public static void ConfigureServicesTestDB<TTrxn, TQuery>(IServiceCollection services, string? dbConnectionString, string dbName = "TestDB")
        where TTrxn : DbContext
        where TQuery : DbContext
    {
        var logger = StaticLogging.CreateLogger("DbSupport");
        //if _dbConnectionString is null, use the api defined DbContext/DB, otherwise switch out the DB here
        if (!string.IsNullOrEmpty(dbConnectionString))
        {
            logger.InfoLog($"Swapping services DbContext to use test database source: {dbConnectionString}");

            //seems DI is lazily creating services at the time of injection, so removing here is not effective,
            //removing IDbContextOptionsConfiguration seems to help for in memory - otherwise both sql server and in-memory will be registered, which causes problems in the api
            services.RemoveAll<DbContextOptions<TTrxn>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TTrxn>>();
            services.RemoveAll<TTrxn>();
            services.RemoveAll<DbContextOptions<TQuery>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TQuery>>();
            services.RemoveAll<TQuery>();

            services.AddDbContext<TTrxn>((sp, opt) =>
            {
                if (dbConnectionString == "UseInMemoryDatabase")
                {
                    opt.UseInMemoryDatabase(dbName);
                }
                else
                {
                    opt.UseSqlServer(dbConnectionString,
                        //retry strategy does not support user initiated transactions 
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        });

                    //todo check service collection for existing key store provider
                    var registeredSqlColumnEncryptionAzureKeyVaultProvider = sp.GetService<SqlColumnEncryptionAzureKeyVaultProvider>();
                    if (registeredSqlColumnEncryptionAzureKeyVaultProvider == null)
                    {
                        //sql always encrypted support; connection string must include "Column Encryption Setting=Enabled"
                        var credential = new DefaultAzureCredential();
                        SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);
                        try
                        {
                            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                            {
                                {
                                    SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, sqlColumnEncryptionAzureKeyVaultProvider
                                }
                            });
                        }
                        catch
                        {
                            //ignore; already registered.  T
                            //this is a workaround for the fact that the SqlColumnEncryptionAzureKeyVaultProvider is already registered in the web application factory registrations
                            //SqlConnection does not currently have a Try register method or any way to check if a provider is already registered
                        }

                    }

                    //if (!_keyStoreProviderRegistered)
                    //{
                    //    //sql always encrypted support; connection string must include "Column Encryption Setting=Enabled"
                    //    var credential = new DefaultAzureCredential();
                    //    SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);

                    //    try
                    //    {
                    //        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                    //        {
                    //            {
                    //                SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, sqlColumnEncryptionAzureKeyVaultProvider
                    //            }
                    //        });
                    //    }
                    //    catch
                    //    {
                    //        //ignore; already registered.  T
                    //        //this is a workaround for the fact that the SqlColumnEncryptionAzureKeyVaultProvider is already registered in the web application factory registrations
                    //        //SqlConnection does not currently have a Try register method or any way to check if a provider is already registered
                    //    }
                    //    _keyStoreProviderRegistered = true;
                    //}
                }
            }, ServiceLifetime.Singleton, ServiceLifetime.Singleton);

            services.AddDbContext<TQuery>((sp, opt) =>
            {
                if (dbConnectionString == "UseInMemoryDatabase")
                {
                    opt.UseInMemoryDatabase(dbName);
                }
                else
                {
                    opt.UseSqlServer(dbConnectionString,
                        //retry strategy does not support user initiated transactions 
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        });

                    if (!_keyStoreProviderRegistered)
                    {
                        //sql always encrypted support; connection string must include "Column Encryption Setting=Enabled"
                        var credential = new DefaultAzureCredential();
                        SqlColumnEncryptionAzureKeyVaultProvider sqlColumnEncryptionAzureKeyVaultProvider = new(credential);

                        try
                        {
                            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                            {
                                {
                                    SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, sqlColumnEncryptionAzureKeyVaultProvider
                                }
                            });
                        }
                        catch
                        {
                            //ignore; already registered.  T
                            //this is a workaround for the fact that the SqlColumnEncryptionAzureKeyVaultProvider is already registered in the web application factory registrations
                            //SqlConnection does not currently have a Try register method or any way to check if a provider is already registered
                        }
                        _keyStoreProviderRegistered = true;
                    }
                }
            }, ServiceLifetime.Singleton, ServiceLifetime.Singleton);
        }
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
