using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using SampleApp.Bootstrapper;
using Testcontainers.MsSql;

namespace Test.Integration;

/// <summary>
/// Testing Application and Domain services and logic; not http endpoints
/// </summary>
public abstract class IntegrationTestBase
{
    protected const string ClientName = "IntegrationTest";

    protected readonly static IConfigurationRoot Config = Support.Utility.BuildConfiguration().AddUserSecrets<IntegrationTestBase>().Build();

    protected readonly IServiceProvider Services;
    protected readonly ILogger<IntegrationTestBase> Logger;

    //https://testcontainers.com/guides/testing-an-aspnet-core-web-app/
    public static readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    protected IntegrationTestBase()
    {
        //Services for DI
        ServiceCollection services = [];

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().AddDebug().AddApplicationInsights();
        });
        services.AddSingleton(loggerFactory);

        //bootstrapper service registrations - infrastructure, domain, application 
        services
            .RegisterInfrastructureServices(Config)
            .RegisterBackgroundServices(Config)
            .RegisterDomainServices(Config)
            .RegisterApplicationServices(Config);

        //replace api registered services with test versions
        var dbSource = Config.GetValue<string?>("TestSettings:DBSource", null);

        //if dbSource is null, use the api defined DbContext/DB, otherwise switch out the DB here
        if (!string.IsNullOrEmpty(dbSource))
        {
            services.RemoveAll(typeof(DbContextOptions<TodoDbContextTrxn>));
            services.RemoveAll(typeof(TodoDbContextTrxn));
            services.AddDbContext<TodoDbContextTrxn>(options =>
            {
                if (dbSource == "TestContainer")
                {
                    //use sql server test container
                    options.UseSqlServer(_dbContainer.GetConnectionString(),
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
        db.Database.EnsureCreated();

        //Seed Data
        if (Config.GetValue("TestSettings:SeedData", false))
        {
            try
            {
                Support.Utility.SeedDefaultEntityData(db);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred seeding the database with test data. Error: {ex.Message}");
            }
        }


        //IRequestContext - replace the Bootstrapper registered non-http 'BackgroundService' registration; injected into repositories
        services.AddTransient<IRequestContext<string>>(provider =>
        {
            var correlationId = Guid.NewGuid().ToString();
            return new RequestContext<string>(correlationId, $"Test.Integration-{correlationId}");
        });

        //build IServiceProvider for subsequent use finding/injecting services
        Services = services.BuildServiceProvider(validateScopes: true);

        //add logging for integration tests
        services.AddLogging(configure => configure.ClearProviders().AddConsole().AddDebug().AddApplicationInsights());
        Logger = Services.GetRequiredService<ILogger<IntegrationTestBase>>();

        Logger.Log(LogLevel.Information, "Test Initialized.");
    }

    [AssemblyInitialize]
    public static void Initialize(TestContext ctx)
    {
        ctx.GetHashCode();
    }


}
