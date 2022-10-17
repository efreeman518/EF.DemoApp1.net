using Application.Contracts.Model;
using Application.Contracts.Services;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Utility.Exceptions;
using System;
using System.Threading.Tasks;

namespace Test.Integration.Application;

[TestClass]
public class TodoServiceTests : ServiceTestBase
{
    public TodoServiceTests() : base()
    { }

    [TestMethod]
    public async Task Todo_CRUD_pass()
    {
        Logger.Log(LogLevel.Information, "Starting ContextEntity_crud_pass");

        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services

        //arrange
        string name = $"Entity a {Guid.NewGuid()}";
        TodoService svc = (TodoService)serviceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));

        TodoItemDto? todo = new()
        {
            Name = name,
            IsComplete = false
        };

        //act & assert

        //create
        todo = await svc.AddItemAsync(todo);
        Assert.IsTrue(todo.Id != Guid.Empty);
        var id = todo.Id;

        //retrieve
        todo = await svc.GetItemAsync(id);
        Assert.AreEqual(id, todo.Id);

        //update
        bool isComplete = true;
        string newName = "mow lawn";
        todo.IsComplete = isComplete;
        todo.Name = newName;
        var updated = await svc.UpdateItemAsync(todo);
        Assert.AreEqual(isComplete, updated?.IsComplete);
        Assert.AreEqual(newName, updated?.Name);

        //delete
        await svc.DeleteItemAsync(id);

        //ensure NotFoundException after delete
        try
        {
            _ = svc.GetItemAsync(id);
        }
        catch (NotFoundException ex)
        {
            Assert.IsTrue(ex != null);
        }

    }

}
