using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Domain.Shared.Enums;
using Infrastructure.SampleApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Integration.Application;

[Ignore("SampleApi must be running somewhere, along with any test side credentials required (in config settings).")]

[TestClass]
public class SampleApiRestClientTests : IntegrationTestBase
{
    public SampleApiRestClientTests() : base()
    { }

    [TestMethod]
    public async Task CRUD_pass()
    {
        //arrange
        string name = $"Todo-a-{Guid.NewGuid()}";
        var todo = new TodoItemDto(null, name, TodoItemStatus.Created);

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        SampleApiRestClient svc = (SampleApiRestClient)serviceScope.ServiceProvider.GetRequiredService(typeof(ISampleApiRestClient));

        //act

        //POST create (insert)
        var todoResponse = await svc.SaveItemAsync(todo);
        Assert.IsNotNull(todoResponse);

        if (!Guid.TryParse(todoResponse!.Id.ToString(), out Guid id)) throw new Exception("Invalid Guid");
        Assert.IsNotNull(id);

        //GET retrieve
        todoResponse = await svc.GetItemAsync(id);
        Assert.AreEqual(id, todoResponse!.Id);

        //PUT update
        todo = todoResponse;
        var todo2 = todo with { Name = $"Update {name}" };
        todoResponse = await svc.SaveItemAsync(todo2)!;
        Assert.AreEqual(todo2.Name, todoResponse!.Name);

        //GET retrieve
        todoResponse = await svc.GetItemAsync(id);
        Assert.AreEqual(todo2.Name, todoResponse!.Name);

        //DELETE
        await svc.DeleteItemAsync(id);

        //GET (NotFound) - ensure deleted - NotFound exception expected
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => await svc.GetItemAsync(id));
    }

    [ClassInitialize]
    public static async Task ClassInit(TestContext testContext)
    {
        Console.WriteLine(testContext.TestName);
        await _dbContainer.StartAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _dbContainer.StopAsync();
    }
}
