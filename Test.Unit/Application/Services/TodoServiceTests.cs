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
using Package.Infrastructure.Common.Exceptions;
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
    private readonly Mock<ISampleApiRestClient> SampleApiRestClientMock;
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
        //ValidationHelperMock = _mockFactory.Create<IValidationHelper>();
        RepositoryQueryMock = _mockFactory.Create<ITodoRepositoryQuery>();
        RepositoryTrxnMock = _mockFactory.Create<ITodoRepositoryTrxn>();
        SampleApiRestClientMock = _mockFactory.Create<ISampleApiRestClient>();
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

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        _repoTrxn = new TodoRepositoryTrxn(dbTrxn, src);
        _repoQuery = new TodoRepositoryQuery(dbQuery, src); //not used in this test
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
            .Callback(new MockCreateCallback((ref TodoItem output) => output = dbTodo));

        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object,
            RepositoryTrxnMock.Object, RepositoryQueryMock.Object, SampleApiRestClientMock.Object, BackgroundTaskQueueMock.Object);

        //act & assert

        //create
        var todoNew = new TodoItemDto(null, name, TodoItemStatus.Created);
        TodoItemDto? todoSaved = null;
        var result = await svc.CreateItemAsync(todoNew); //new Id has been assigned
        _ = result.Match<TodoItemDto?>(
            dto => todoSaved = dto,
            err => null
        );
        Assert.IsTrue(todoNew.Id != todoSaved!.Id); //orig dto will still have empty Guid id
        Assert.IsTrue(todoNew.Name == todoSaved!.Name);
        Assert.AreEqual(todoNew.Status, todoSaved.Status);

        var id = (Guid)todoSaved.Id!;

        //retrieve
        var oTodo = await svc.GetItemAsync(id);
        var todoGet = oTodo.Match(
            dto => dto,
            () => throw new AssertFailedException("TodoItem not found")
        );
        Assert.AreEqual(todoSaved.Id, todoGet!.Id);
        Assert.AreEqual(todoSaved.Name, todoGet!.Name);

        //update
        TodoItemDto? todoUpdated = null;
        result = await svc.UpdateItemAsync(todoGet!); //mock set to return a specific todoItem
        _ = result.Match(
            dto => todoUpdated = dto,
            err => throw err
        );
        Assert.IsTrue(todoUpdated != null);

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
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var todo = new TodoItemDto(null, "wash car", TodoItemStatus.Created);

        //act & assert

        //create
        var result = await svc.CreateItemAsync(todo);
        _ = result.Match(
            dto => todo = dto,
            err => throw err
        );
        Assert.IsTrue(todo.Id != Guid.Empty);
        var id = (Guid)todo.Id!;

        //retrieve
        var oTodo = await svc.GetItemAsync(id);
        todo = oTodo.Match(
            dto => dto,
            () => throw new AssertFailedException("TodoItem not found")
        );
        Assert.AreEqual(id, todo!.Id);

        //update
        string newName = "mow lawn";
        var todo2 = todo with { Name = newName, Status = TodoItemStatus.Completed };

        TodoItemDto? updated = null;
        result = await svc.UpdateItemAsync(todo2);
        _ = result.Match(
            dto => updated = dto,
            err => throw err
        );
        Assert.AreEqual(TodoItemStatus.Completed, updated?.Status);
        Assert.AreEqual(newName, updated?.Name);

        //retrieve and make sure the update persisted
        oTodo = await svc.GetItemAsync(id);
        todo = oTodo.Match(
            dto => dto,
            () => throw new AssertFailedException("TodoItem not found")
        );
        Assert.AreEqual(updated!.Status, todo?.Status);

        //delete
        await svc.DeleteItemAsync(id);

        //ensure not found after delete
        oTodo = await svc.GetItemAsync(id);
        Assert.IsTrue(oTodo.IsNone);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("asd")]
    [DataRow("fhjkjfgkhj")]
    public async Task Todo_AddItemAsync_fail(string name)
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var todo = new TodoItemDto(null, name, TodoItemStatus.Created);

        //act & assert
        var result = await svc.CreateItemAsync(todo);
        Assert.IsTrue(result.IsFaulted);
    }

    [TestMethod]
    public async Task Todo_UpdateAsync_notfound()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        //random Id to 'update'
        var todo = new TodoItemDto(Guid.NewGuid(), "asdsa", TodoItemStatus.Created);

        //act & assert
        var result = await svc.UpdateItemAsync(todo);
        Assert.IsTrue(result.IsFaulted);
        result.IfFail(ex => Assert.IsTrue(ex is NotFoundException));
        result.IfSucc(todo => Assert.Fail($"Should not have found id {todo!.Id} to update"));
    }

    [TestMethod]
    public async Task Todo_UpdateAsync_fail()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        //random Id to 'update'
        var todo = new TodoItemDto(Guid.NewGuid(), "sdsa", TodoItemStatus.Created);

        //act & assert
        var result = await svc.UpdateItemAsync(todo);
        Assert.IsTrue(result.IsFaulted);
        result.IfFail(ex => Assert.IsTrue(ex is NotFoundException));
        result.IfSucc(todo => Assert.Fail($"Should not have found id {todo!.Id} to update"));
    }

    [TestMethod]
    public async Task Todo_GetPageAsync_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));

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
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
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
