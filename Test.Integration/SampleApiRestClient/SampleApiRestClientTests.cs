using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Infrastructure.SampleApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Integration.Application;

[Ignore("SampleApi must be running somewhere with any credentials required in config settings.")]

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
        var todo = new TodoItemDto
        {
            Name = name
        };

        //arrange
        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services
        SampleApiRestClient svc = (SampleApiRestClient)serviceScope.ServiceProvider.GetRequiredService(typeof(ISampleApiRestClient));

        //act

        //POST create (insert)
        var todoResponse = await svc.SaveItemAsync(todo);
        Assert.IsNotNull(todoResponse);

        if (!Guid.TryParse(todoResponse!.Id.ToString(), out Guid id)) throw new Exception("Invalid Guid");
        Assert.IsTrue(id != Guid.Empty);

        //GET retrieve
        todoResponse = await svc.GetItemAsync(id);
        Assert.AreEqual(id, todoResponse!.Id);

        //PUT update
        todo = todoResponse;
        todo.Name = $"Update {name}";
        todoResponse = await svc.SaveItemAsync(todo)!;
        Assert.AreEqual(todo.Name, todoResponse!.Name);

        //GET retrieve
        todoResponse = await svc.GetItemAsync(id);
        Assert.AreEqual(todo.Name, todoResponse!.Name);

        //DELETE
        await svc.DeleteItemAsync(id);

        //GET (NotFound) - ensure deleted - NotFound exception expected
        try
        {
            todoResponse = await svc.GetItemAsync(id);
        }
        catch (Exception ex)
        {
            Assert.IsNotNull(ex);
        }
    }
}
