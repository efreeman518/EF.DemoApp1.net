using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Extensions;

namespace Test.Support;

public static class Utility
{
    /// <summary>
    /// For loading config for tests since we don't have a host that automatically loads it
    /// </summary>
    /// <param name="path"></param>
    /// <param name="includeEnvironmentVars"></param>
    /// <returns>Config builder for further composition and the environment</returns>
    public static IConfigurationBuilder BuildConfiguration(string? path = "appsettings.json", bool includeEnvironmentVars = true)
    {
        //order matters here (last wins)
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        if (path != null) builder.AddJsonFile(path);
        if (includeEnvironmentVars) builder.AddEnvironmentVariables();

        var config = builder.Build();
        string env = config.GetValue<string>("ASPNETCORE_ENVIRONMENT", "development")!.ToLower();
        builder.AddJsonFile($"appsettings.{env}.json", true);

        return builder;
    }

    public static void ConfigureTestDB(ILogger logger, IServiceCollection services, IConfigurationSection config, string? dbConnectionString = null)
    {
        //replace api registered services with test versions
        var dbSource = config.GetValue<string?>("DBSource", null);

        //if dbSource is null, use the api defined DbContext/DB, otherwise switch out the DB here
        if (!string.IsNullOrEmpty(dbSource))
        {
            logger.InfoLog($"Using test database source: {dbSource}");

            services.RemoveAll(typeof(DbContextOptions<TodoDbContextTrxn>));
            services.RemoveAll(typeof(TodoDbContextTrxn));
            services.AddDbContext<TodoDbContextTrxn>(options =>
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
        var db = scopedServices.GetRequiredService<TodoDbContextTrxn>();

        //Environment.SetEnvironmentVariable("AKVCMKURL", "");
        //db.Database.Migrate(); //needs AKVCMKURL env var set
        db.Database.EnsureCreated(); //does not use migrations; uses DbContext to create tables

        var seedPaths = config.GetSection("SeedFiles:Paths").Get<string[]>();
        if (seedPaths != null && seedPaths.Length > 0)
        {
            db.SeedRawSqlFiles(logger, [.. seedPaths], config.GetValue("SeedFiles:SearchPattern", "*.sql")!);
        }

        //Seed Data
        if (config.GetValue("SeedData", false))
        {
            try
            {
                logger.InfoLog($"Seeding default entity data.");
                db.SeedDefaultEntityData(false);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the database with test data. Error: {Message}", ex.Message);
            }
        }
    }

    public static void SeedDefaultEntityData(this TodoDbContextBase db, bool clear = true)
    {
        if (clear) db.Set<TodoItem>().RemoveRange(db.Set<TodoItem>());
        db.Set<TodoItem>().AddRange(new List<TodoItem>
        {
            new("item1a", TodoItemStatus.Created) { CreatedBy = "Test.Unit" },
            new("item2a", TodoItemStatus.InProgress) { CreatedBy = "Test.Unit" },
            new("item3a", TodoItemStatus.Completed){ CreatedBy = "Test.Unit" }
        });
    }

    public static void SeedRawSqlFiles(this TodoDbContextBase db, ILogger logger, List<string> relativePaths, string searhPattern)
    {
        relativePaths.ForEach(path =>
        {
            string[] files = [.. Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path), searhPattern).OrderBy(f => f)]; //order by name
            foreach (var filePath in files)
            {
                db.SeedRawSqlFile(logger, filePath);
            }
        });
    }

    public static void SeedRawSqlFile(this TodoDbContextBase db, ILogger logger, string filePath)
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

    public static List<TodoItem> TodoItemListFactory(int size, TodoItemStatus? status = null)
    {
        var list = new List<TodoItem>();
        for (int i = 0; i < size; i++)
        {
            list.Add(TodoItemFactory($"a some entity {i}", status ?? RandomEnumValue<TodoItemStatus>()));
        }
        return list;
    }

    public static TodoItem TodoItemFactory(string name, TodoItemStatus? status = null, DateTime? createdDate = null)
    {
        return new TodoItem(name, status ?? RandomEnumValue<TodoItemStatus>()) { CreatedBy = "Test.Unit", CreatedDate = createdDate ?? DateTime.UtcNow };
    }

    private static readonly Random _R = new();
    private static TEnum? RandomEnumValue<TEnum>()
    {
        var v = Enum.GetValues(typeof(TEnum));
        return (TEnum?)v.GetValue(_R.Next(v.Length));
    }

}
