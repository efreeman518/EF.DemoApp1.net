using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Extensions;

namespace Test.Support;
public static class DbSupport
{
    public static T ConfigureTestDB<T>(ILogger logger, IServiceCollection services, string? dbSource, string? dbConnectionString = null)
        where T : DbContext
    {
        //if dbSource is null, use the api defined DbContext/DB, otherwise switch out the DB here
        if (!string.IsNullOrEmpty(dbSource))
        {
            logger.InfoLog($"Using test database source: {dbSource}");

            services.RemoveAll(typeof(DbContextOptions<T>));
            services.RemoveAll(typeof(T));
            services.AddDbContext<T>(options =>
            {
                if (dbSource == "TestContainer")
                {
                    //use sql server test container
                    options.UseSqlServer(dbConnectionString,
                        //retry strategy does not support user initiated transactions 
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        });
                }
                else if (dbSource == "UseInMemoryDatabase")
                {
                    options.UseInMemoryDatabase($"Test.Endpoints-{Guid.NewGuid()}");
                }
                else
                {
                    options.UseSqlServer(dbSource,
                        //retry strategy does not support user initiated transactions 
                        sqlServerOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        });
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

    public static void SeedRawSqlFiles(this DbContext db, ILogger logger, List<string> relativePaths, string searchPattern)
    {
        relativePaths.ForEach(path =>
        {
            string[] files = [.. Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path), searchPattern).OrderBy(f => f)]; //order by name
            foreach (var filePath in files)
            {
                db.SeedRawSqlFile(logger, filePath);
            }
        });
    }

    public static void SeedRawSqlFile(this DbContext db, ILogger logger, string filePath)
    {
        try
        {
            logger.InfoLog($"Seeding test database from file: {filePath}");
            var sql = File.ReadAllText(filePath);
            db.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception ex)
        {
            logger.ErrorLog($"An error occurred seeding the database from file {filePath}", ex);
        }
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
