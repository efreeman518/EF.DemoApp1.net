using Application.Contracts.Model;
using Domain.Shared.Enums;
using Infrastructure.SampleApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Extensions;

namespace Test.Integration.SampleApiRestClient;

[Ignore("SampleApi must be running somewhere, along with any test side credentials required (in config settings).")]

[TestClass]
public class SampleApiRestClientTests
{
    private readonly ILogger<SampleApiRestClientTests> _logger;
    private readonly Infrastructure.SampleApi.SampleApiRestClient _svc;
    private readonly HttpClient _httpClient;

    public SampleApiRestClientTests()
    {
        IConfigurationRoot config = Support.Utility.BuildConfiguration().AddUserSecrets<SampleApiRestClientTests>().Build();

        //logger
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().AddDebug().AddApplicationInsights();
        });
        _logger = loggerFactory.CreateLogger<SampleApiRestClientTests>();

        //settings
        SampleApiRestClientSettings settings = new();
        config.GetSection(SampleApiRestClientSettings.ConfigSectionName).Bind(settings);
        var oSettings = Options.Create(settings);

        if (config.GetValue<string>("SampleApiRestClientSettings:ClientId") != null)
        {
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", config.GetValue<string>("SampleApiRestClientSettings:TenantId"));
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", config.GetValue<string>("SampleApiRestClientSettings:ClientId"));
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", config.GetValue<string>("SampleApiRestClientSettings:ClientSecret"));
        }
        var scopes = config.GetSection("SampleApiRestClientSettings:Scopes").Get<string[]>();
        var handler = new SampleRestApiAuthMessageHandler(scopes!)
        {
            InnerHandler = new HttpClientHandler() //required for test which behaves different than runtime startup/registration
        };

        //httpclient
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(config.GetValue<string>("SampleApiRestClientSettings:BaseUrl")!)
        };

        _svc = new Infrastructure.SampleApi.SampleApiRestClient(loggerFactory.CreateLogger<Infrastructure.SampleApi.SampleApiRestClient>(), oSettings, _httpClient);

        _logger.InfoLog("SampleApiRestClientTests - constructor finished.");
    }

    [TestMethod]
    public async Task CRUD_pass()
    {
        //arrange
        string name = $"Todo-a-{Guid.NewGuid()}";
        var todo = new TodoItemDto(null, name, TodoItemStatus.Created);

        //act

        //POST create (insert)
        //var todoResponse = await _svc.SaveItemAsync(todo);

        var result = await _svc.SaveItemAsync(todo);
        var todoResponse = result.Match(
            Succ: dto => dto,
            Fail: err => throw err
            );
        Assert.IsNotNull(todoResponse);

        if (!Guid.TryParse(todoResponse!.Id.ToString(), out Guid id)) throw new Exception("Invalid Guid");
        Assert.IsNotNull(id);

        //GET retrieve
        todoResponse = await _svc.GetItemAsync(id);
        Assert.AreEqual(id, todoResponse!.Id);

        //PUT update
        todo = todoResponse;
        var todo2 = todo with { Name = $"Update {name}" };
        //todoResponse = await _svc.SaveItemAsync(todo2)!;
        todoResponse = result.Match(
            Succ: dto => dto,
            Fail: err => throw err
            );
        Assert.AreEqual(todo2.Name, todoResponse!.Name);

        //GET retrieve
        todoResponse = await _svc.GetItemAsync(id);
        Assert.AreEqual(todo2.Name, todoResponse!.Name);

        //DELETE
        await _svc.DeleteItemAsync(id);

        //GET (NotFound) - ensure deleted - NotFound exception expected
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => await _svc.GetItemAsync(id));
    }
}
