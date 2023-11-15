using Application.Contracts.Model;
using Application.Contracts.Services;
using Application.Services;
using Domain.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Exceptions;

namespace Test.Integration.Application;

[TestClass]
public class TodoServiceTests : IntegrationTestBase
{
    public TodoServiceTests() : base()
    { }

    [TestMethod]
    public async Task Todo_CRUD_pass()
    {
        Logger.Log(LogLevel.Information, "Starting Todo_CRUD_pass");

        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services

        //arrange
        string name = $"Entity a {Guid.NewGuid()}";
        TodoService svc = (TodoService)serviceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));

        TodoItemDto? todo = new()
        {
            Name = name,
            Status = TodoItemStatus.Created
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
        string newName = "mow lawn";
        todo.Status = TodoItemStatus.Completed;
        todo.Name = newName;
        var updated = await svc.UpdateItemAsync(todo);
        Assert.AreEqual(TodoItemStatus.Completed, updated!.Status);
#pragma warning disable S2589 // Boolean expressions should not be gratuitous - FALSE POSITIVE
        Assert.AreEqual(newName, updated?.Name);
#pragma warning restore S2589 // Boolean expressions should not be gratuitous

        //retrieve and make sure the update persisted
        todo = await svc.GetItemAsync(id);
        Assert.AreEqual(updated!.Status, todo.Status);

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

    [DataTestMethod]
    [DataRow("asg")]
    [DataRow("sdfg")]
    [DataRow("sdfgsd456yrt")]
    [DataRow("sdfgs")]
    [ExpectedException(typeof(FluentValidation.ValidationException))]
    public async Task Todo_AddItem_fail(string name)
    {
        Logger.Log(LogLevel.Information, "Starting Todo_AddItem_fail");

        using IServiceScope serviceScope = Services.CreateScope(); //needed for injecting scoped services

        //arrange
        TodoService svc = (TodoService)serviceScope.ServiceProvider.GetRequiredService(typeof(ITodoService));
        TodoItemDto? todo = new()
        {
            Name = name,
            Status = TodoItemStatus.Created
        };

        //act & assert

        //create
        await svc.AddItemAsync(todo);
    }

}
