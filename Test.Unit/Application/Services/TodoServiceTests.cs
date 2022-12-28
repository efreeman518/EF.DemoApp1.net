using Application.Contracts.Model;
using Application.Services;
using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Package.Infrastructure.Data.Contracts;
using Package.Infrastructure.Utility.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Test.Support;

namespace Test.Unit.Application.Services;

[TestClass]
public class TodoServiceTests : UnitTestBase
{
    //specific to this test class
    private readonly Mock<ITodoRepository> RepositoryMock;
    private readonly IOptions<TodoServiceSettings> _settings = Options.Create(new TodoServiceSettings());

    public TodoServiceTests() : base()
    {
        //use Mock repo
        RepositoryMock = _mockFactory.Create<ITodoRepository>();
        RepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(1)); //default behavior

        //or use DbContext with InMemory provider (dependencies on EF, InMemoryProvider, Infrastructure.Data, Infrastructure.Repositories
        ServiceCollection services = new();
        services.AddTransient<ITodoRepository, TodoRepository>();
    }

    delegate void MockCreateCallback(ref TodoItem output);

    [TestMethod]
    public async Task Todo_CRUD_mock_pass()
    {
        //arrange
        string name = "wash car";
        var dbTodo = new TodoItem { Id = Guid.NewGuid(), Name = name, Status = TodoItemStatus.Accepted };

        RepositoryMock.Setup(
            r => r.GetItemAsync(It.IsAny<bool>(), It.IsAny<Expression<Func<TodoItem, bool>>>(),
                It.IsAny<Func<IQueryable<TodoItem>, IOrderedQueryable<TodoItem>>>(), It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<TodoItem>,IIncludableQueryable<TodoItem, object?>>[]>()))
            .Returns(() => Task.FromResult<TodoItem?>(dbTodo));


        RepositoryMock.Setup(m => m.Create(ref It.Ref<TodoItem>.IsAny))
            .Callback(new MockCreateCallback((ref TodoItem output) => output = dbTodo));

        var svc = new TodoService(new NullLogger<TodoService>(), _settings, RepositoryMock.Object, _mapper);

        //act & assert

        //create
        var todoNew = new TodoItemDto { Name = name, IsComplete = false };
        var todoSaved = await svc.AddItemAsync(todoNew);
        Assert.IsTrue(dbTodo.Id == todoSaved.Id);
        Assert.IsTrue(todoNew.Name == todoSaved.Name);
        Assert.IsTrue(todoNew.IsComplete == todoSaved.IsComplete);

        //retrieve
        var todoGet = await svc.GetItemAsync(todoSaved.Id);
        Assert.AreEqual(todoSaved.Id, todoGet?.Id);
        Assert.AreEqual(todoSaved.Name, todoGet?.Name);

        //update
        TodoItemDto? todoUpdated = await svc.UpdateItemAsync(todoGet!); //mock set to return a specific todoItem
        Assert.IsTrue(todoUpdated != null);

        //delete
        await svc.DeleteItemAsync(todoUpdated!.Id);

        //verify call counts
        RepositoryMock.Verify(
            r => r.Create(ref It.Ref<TodoItem>.IsAny),
            Times.Once);
        RepositoryMock.Verify(
           r => r.GetItemAsync(It.IsAny<bool>(), It.IsAny<Expression<Func<TodoItem, bool>>>(),
               It.IsAny<Func<IQueryable<TodoItem>, IOrderedQueryable<TodoItem>>>(), It.IsAny<CancellationToken>(),
               It.IsAny<Func<IQueryable<TodoItem>, IIncludableQueryable<TodoItem, object?>>[]>()),
           Times.Exactly(2)); //called for Update and Get
        RepositoryMock.Verify(
            r => r.UpdateFull(ref It.Ref<TodoItem>.IsAny),
            Times.Once);
        RepositoryMock.Verify(
            r => r.Delete(It.IsAny<TodoItem>()),
            Times.Once);

    }

    [TestMethod]
    public async Task Todo_CRUD_memory_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoContext db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities =>
            {
                //custom data scenario that default seed data does not cover
                entities.Add(new TodoItem { Id = Guid.NewGuid(), Name = "some entity", CreatedBy = "unit test", UpdatedBy = "unit test", CreatedDate = DateTime.UtcNow });
            })
            .GetOrBuild<TodoContext>();

        ITodoRepository repo = new TodoRepository(db, _mapper, new AuditDetail("Test.Unit"));
        var svc = new TodoService(new NullLogger<TodoService>(), _settings, repo, _mapper);
        var todo = new TodoItemDto { Name = "wash car", IsComplete = false };

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
