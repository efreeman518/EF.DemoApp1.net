using Application.Contracts.Interfaces;
using Application.Contracts.Model;
using Application.Services;
using Application.Services.Validators;
using Domain.Model;
using Domain.Shared.Enums;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Package.Infrastructure.BackgroundServices;
using Package.Infrastructure.Common;
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
    private readonly Mock<IValidationHelper> ValidationHelperMock;
    private readonly Mock<ITodoRepositoryTrxn> RepositoryTrxnMock;
    private readonly Mock<ITodoRepositoryQuery> RepositoryQueryMock;
    private readonly Mock<ISampleApiRestClient> SampleApiRestClientMock;
    private readonly Mock<IBackgroundTaskQueue> BackgroundTaskQueueMock;
    private readonly Mock<IOptionsMonitor<TodoServiceSettings>> SettingsMock;
    private readonly TodoServiceSettings _settings = new();
    private readonly ITodoRepositoryTrxn _repoTrxn;
    private readonly ITodoRepositoryQuery _repoQuery;
    private readonly IValidationHelper _validationHelper;

    public TodoServiceTests() : base()
    {
        //use Mock repo
        ServiceScopeFactoryMock = _mockFactory.Create<IServiceScopeFactory>();
        ValidationHelperMock = _mockFactory.Create<IValidationHelper>();
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
        ServiceCollection services = new();
        services.AddTransient<ITodoRepositoryTrxn, TodoRepositoryTrxn>();

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
                entities.Add(new TodoItem("some entity a"));
                entities.Add(new TodoItem("some other entity a"));
            })
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        _repoTrxn = new TodoRepositoryTrxn(dbTrxn, src);
        _repoQuery = new TodoRepositoryQuery(dbQuery, src, _mapper); //not used in this test

        services.AddTransient<IValidationHelper, ValidationHelper>();
        services.AddTransient<IValidator<TodoItemDto>, TodoItemDtoValidator>();
        services.AddTransient<ITodoRepositoryQuery, TodoRepositoryQuery>(provider => (TodoRepositoryQuery)_repoQuery);
        _validationHelper = new ValidationHelper(services.BuildServiceProvider());

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

        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, ValidationHelperMock.Object,
            RepositoryTrxnMock.Object, RepositoryQueryMock.Object, SampleApiRestClientMock.Object,
            _mapper, BackgroundTaskQueueMock.Object);

        //act & assert

        //create
        var todoNew = new TodoItemDto { Name = name, Status = TodoItemStatus.Created };
        var todoSaved = await svc.AddItemAsync(todoNew); //new Id has been assigned
        Assert.IsTrue(todoNew.Id != todoSaved.Id); //orig dto will still have empty Guid id
        Assert.IsTrue(todoNew.Name == todoSaved.Name);
        Assert.AreEqual(todoNew.Status, todoSaved.Status);

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
        RepositoryTrxnMock.Verify(
            r => r.Create(ref It.Ref<TodoItem>.IsAny),
            Times.Once);
        RepositoryTrxnMock.Verify(
           r => r.GetEntityAsync(It.IsAny<bool>(), It.IsAny<Expression<Func<TodoItem, bool>>>(),
               It.IsAny<Func<IQueryable<TodoItem>, IOrderedQueryable<TodoItem>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(),
               It.IsAny<Func<IQueryable<TodoItem>, IIncludableQueryable<TodoItem, object?>>[]>()),
           Times.Exactly(2)); //called for Update and Get
        RepositoryTrxnMock.Verify(
            r => r.DeleteAsync<TodoItem>(It.IsAny<Guid>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Todo_CRUD_memory_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var todo = new TodoItemDto { Name = "wash car", Status = TodoItemStatus.Created };

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
        Assert.AreEqual(TodoItemStatus.Completed, updated?.Status);
        Assert.AreEqual(newName, updated?.Name);

        //retrieve and make sure the update persisted
        todo = await svc.GetItemAsync(id);
        Assert.AreEqual(updated!.Status, todo.Status);

        //delete
        await svc.DeleteItemAsync(id);

        //ensure NotFoundException after delete
        try
        {
            _ = await svc.GetItemAsync(id);
            throw new Exception("Should not have found a deleted item.");
        }
        catch (NotFoundException ex)
        {
            Assert.IsTrue(ex != null);
        }
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("asd")]
    [DataRow("fhjkjfgkhj")]
    [ExpectedException(typeof(FluentValidation.ValidationException))]
    public async Task Todo_AddItemAsync_fail(string name)
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var todo = new TodoItemDto { Name = name, Status = TodoItemStatus.Created };

        //act & assert
        await svc.AddItemAsync(todo);
    }

    [TestMethod]
    [ExpectedException(typeof(NotFoundException))]
    public async Task Todo_UpdateAsync_notfound()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        //random Id to 'update'
        var todo = new TodoItemDto { Id = Guid.NewGuid(), Name = "asdsa", Status = TodoItemStatus.Created };

        //act & assert
        await svc.UpdateItemAsync(todo);
    }

    [TestMethod]
    [ExpectedException(typeof(FluentValidation.ValidationException))]
    public async Task Todo_UpdateAsync_fail()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        //random Id to 'update'
        var todo = new TodoItemDto { Id = Guid.NewGuid(), Name = "sdsa", Status = TodoItemStatus.Created };

        //act & assert
        await svc.UpdateItemAsync(todo);
    }

    [TestMethod]
    public async Task Todo_GetPageAsync_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));

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
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));
        var search = new SearchRequest<TodoItemSearchFilter>()
        {
            Filter = new TodoItemSearchFilter() { Statuses = [TodoItemStatus.Created, TodoItemStatus.Completed] }
        };

        //act
        var response = await svc.SearchAsync(search);

        //act & assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Data.Count <= response.Total);
    }

    [TestMethod]
    public async Task Todo_GetPageExternalAsync_pass()
    {
        //arrange
        var svc = new TodoService(new NullLogger<TodoService>(), SettingsMock.Object, _validationHelper, _repoTrxn, _repoQuery, SampleApiRestClientMock.Object, _mapper, new BackgroundTaskQueue(ServiceScopeFactoryMock.Object));

        //act
        var response = await svc.GetPageExternalAsync();

        //act & assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Data.Count <= response.Total);
    }
}
