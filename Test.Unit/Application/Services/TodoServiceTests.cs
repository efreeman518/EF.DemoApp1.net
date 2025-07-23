using Application.Contracts.Interfaces;
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
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;
using Test.Support;

namespace Test.Unit.Application.Services;

[TestClass]
public class TodoServiceTests : UnitTestBase
{
    //specific to this test class
    private readonly Mock<IServiceScopeFactory> ServiceScopeFactoryMock;
    private readonly Mock<ITodoRepositoryTrxn> RepositoryTrxnMock;
    private readonly Mock<ITodoRepositoryQuery> RepositoryQueryMock;
    private readonly Mock<IBackgroundTaskQueue> BackgroundTaskQueueMock;
    private readonly Mock<IOptionsMonitor<TodoServiceSettings>> SettingsMock;
    private readonly TodoServiceSettings _settings = new();
    private readonly ITodoRepositoryTrxn _repoTrxn;
    private readonly ITodoRepositoryQuery _repoQuery;

    private readonly ServiceCollection _services = new();

    public TodoServiceTests() : base()
    {
        //use Mock repo
        ServiceScopeFactoryMock = _mockFactory.Create<IServiceScopeFactory>();
        RepositoryQueryMock = _mockFactory.Create<ITodoRepositoryQuery>();
        RepositoryTrxnMock = _mockFactory.Create<ITodoRepositoryTrxn>();
        BackgroundTaskQueueMock = _mockFactory.Create<IBackgroundTaskQueue>();
        RepositoryTrxnMock.Setup(r => r.SaveChangesAsync(It.IsAny<OptimisticConcurrencyWinner>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(1)); //default behavior

        //set any _settings
        _settings.StringProperty = "some value";
        SettingsMock = _mockFactory.Create<IOptionsMonitor<TodoServiceSettings>>();
        SettingsMock.Setup(m => m.CurrentValue).Returns(_settings);

        //or use DbContext with InMemory provider (dependencies on EF, InMemoryProvider, Infrastructure.Data, Infrastructure.Repositories
        //ServiceCollection services = new();
        _services.AddTransient<ITodoRepositoryTrxn, TodoRepositoryTrxn>();

        //arrange default for some tests
        //InMemory setup & seed
        var dbTrxn = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .BuildInMemory<TodoDbContextTrxn>();
        var dbQuery = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities =>
            {
                //custom data scenario that default seed data does not cover
                entities.Add(TodoDbContextSupport.TodoItemFactory("some entity a"));
                entities.Add(TodoDbContextSupport.TodoItemFactory("some other entity a"));
            })
            .BuildInMemory<TodoDbContextQuery>();

        //var src = new RequestContext<string, string>(Guid.NewGuid().ToString(), "Test.Unit", null);
        _repoTrxn = new TodoRepositoryTrxn(dbTrxn);
        _repoQuery = new TodoRepositoryQuery(dbQuery); //not used in this test
    }

    delegate void MockCreateCallback(ref TodoItem output);

    [TestMethod]
    public async Task Todo_CRUD_mock_pass()
    {
        //arrange return from repo
        string name = "wash car";
        var dbTodo = new TodoItem(name, TodoItemStatus.Created);

        RepositoryTrxnMock.Setup(
            r => r.GetEntityAsync(It.IsAny<bool>(), It.IsAny<Expression<Func<TodoItem, bool>>>(),
                It.IsAny<Func<IQueryable<TodoItem>, IOrderedQueryable<TodoItem>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<TodoItem>, IIncludableQueryable<TodoItem, object?>>[]>()))
            .Returns(() => Task.FromResult<TodoItem?>(dbTodo));


        RepositoryTrxnMock.Setup(m => m.Create(ref It.Ref<TodoItem>.IsAny))
            .Callback(new MockCreateCallback((ref output) => output = dbTodo));

        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object,
            RepositoryTrxnMock.Object, RepositoryQueryMock.Object, BackgroundTaskQueueMock.Object);

        //act & assert

        /// Create
        var todoNew = new TodoItemDto(null, name, TodoItemStatus.Created);
        var result = await svc.CreateItemAsync(todoNew);
        Assert.IsTrue(result.IsSuccess, $"Create failed: {string.Join(",", result.Errors)}");
        var todoSaved = result.Value;
        Assert.IsNotNull(todoSaved);
        Assert.AreNotEqual(todoNew.Id, todoSaved.Id);
        Assert.AreEqual(todoSaved.Name, todoNew.Name);
        Assert.AreEqual(todoNew.Status, todoSaved.Status);

        var id = (Guid)todoSaved.Id!;

        // Retrieve
        var getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsSuccess, $"Retrieve failed: {string.Join(",", getResult.Errors)}");
        var todoGet = getResult.Value;
        Assert.AreEqual(todoSaved.Id, todoGet!.Id);
        Assert.AreEqual(todoSaved.Name, todoGet!.Name);

        // Update
        var updateResult = await svc.UpdateItemAsync(todoGet!);
        Assert.IsTrue(updateResult.IsSuccess, $"Update failed: {string.Join(",", updateResult.Errors)}");
        var todoUpdated = updateResult.Value;
        Assert.IsNotNull(todoUpdated);

        //delete
        await svc.DeleteItemAsync((Guid)todoUpdated.Id!);

        //verify call counts
        RepositoryTrxnMock.Verify(
            r => r.Create(ref It.Ref<TodoItem>.IsAny),
            Times.Once);
        RepositoryTrxnMock.Verify(
           r => r.GetEntityAsync(It.IsAny<bool>(), It.IsAny<Expression<Func<TodoItem, bool>>>(),
               It.IsAny<Func<IQueryable<TodoItem>, IOrderedQueryable<TodoItem>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(),
               It.IsAny<Func<IQueryable<TodoItem>, IIncludableQueryable<TodoItem, object?>>[]>()),
           Times.Exactly(2)); //called for Update and Get
        RepositoryTrxnMock.Verify(
            r => r.DeleteAsync<TodoItem>(It.IsAny<CancellationToken>(), It.IsAny<Guid>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Todo_CRUD_memory_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var todo = new TodoItemDto(null, "wash car", TodoItemStatus.Created);

        //act & assert

        // Create
        var result = await svc.CreateItemAsync(todo);
        Assert.IsTrue(result.IsSuccess, $"Create failed: {string.Join(",", result.Errors)}");
        todo = result.Value;
        Assert.IsNotNull(todo);
        Assert.IsTrue(todo.Id != Guid.Empty);
        var id = (Guid)todo.Id!;

        // Retrieve
        var getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsSuccess, $"Retrieve failed: {string.Join(",", getResult.Errors)}");
        todo = getResult.Value;
        Assert.AreEqual(id, todo!.Id);

        // Update
        string newName = "mow lawn";
        var todo2 = todo with { Name = newName, Status = TodoItemStatus.Completed };

        var updateResult = await svc.UpdateItemAsync(todo2);
        Assert.IsTrue(updateResult.IsSuccess, $"Update failed: {string.Join(",", updateResult.Errors)}");
        var updated = updateResult.Value;
        Assert.AreEqual(TodoItemStatus.Completed, updated?.Status);
        Assert.AreEqual(newName, updated?.Name);

        // Retrieve and ensure the update persisted
        getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsSuccess, $"Retrieve after update failed: {string.Join(",", getResult.Errors)}");
        todo = getResult.Value;
        Assert.AreEqual(updated!.Status, todo?.Status);

        //delete
        await svc.DeleteItemAsync(id);

        /// Ensure not found after delete
        getResult = await svc.GetItemAsync(id);
        Assert.IsTrue(getResult.IsNone, "Item was not deleted");
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("asd")]
    [DataRow("fhjkjfgkhj")]
    public async Task Todo_AddItemAsync_fail(string name)
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var todo = new TodoItemDto(null, name, TodoItemStatus.Created);

        //act & assert
        var result = await svc.CreateItemAsync(todo);
        Assert.IsFalse(result.IsSuccess, "Expected failure but got success");
        Assert.IsNotNull(string.Join(",", result.Errors), "Expected an error message but got none");
    }

    [TestMethod]
    public async Task Todo_UpdateAsync_notfound()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        //random Id to 'update'
        var todo = new TodoItemDto(Guid.NewGuid(), "asdsa", TodoItemStatus.Created);

        //act & assert
        var result = await svc.UpdateItemAsync(todo);
        Assert.IsFalse(result.IsSuccess, "Expected failure but got success");
        Assert.IsNotNull(string.Join(",", result.Errors), "Expected an error message but got none");
    }


    [TestMethod]
    public async Task Todo_GetPageAsync_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));

        //act
        var response = await svc.GetPageAsync();

        //act & assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Data.Count <= response.Total);
    }

    [TestMethod]
    public async Task Todo_SearchPageAsync_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var search = new SearchRequest<TodoItemSearchFilter>()
        {
            Filter = new TodoItemSearchFilter(Statuses: [TodoItemStatus.Created, TodoItemStatus.Completed])
        };

        //act
        var response = await svc.SearchAsync(search);

        //act & assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Data.Count <= response.Total);
    }

}
