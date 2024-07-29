using Application.Contracts.Mappers;
using Application.Contracts.Model;
using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Contracts;
using Package.Infrastructure.Common.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics;
using Test.Support;

namespace Test.Unit.Repository;

[TestClass]
public class TodoRepositoryQueryTests : UnitTestBase
{
    public TodoRepositoryQueryTests() : base()
    {
    }

    [TestMethod]
    public async Task InMemory_SearchTodoItemAsync_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.Add(TodoDbContextSupport.TodoItemFactory("some entity a")))
            .BuildInMemory<TodoDbContextQuery>();

        var rc = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, rc);

        //act & assert
        var search = new SearchRequest<TodoItemSearchFilter> { PageSize = 10, PageIndex = 1 };
        var response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        search = new SearchRequest<TodoItemSearchFilter> { PageSize = 2, PageIndex = 1 };
        response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        search = new SearchRequest<TodoItemSearchFilter> { PageSize = 3, PageIndex = 2 };
        response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task SQLite_SearchTodoItemAsync_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.Add(TodoDbContextSupport.TodoItemFactory("some entity a")))
            .BuildSQLite<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        var search = new SearchRequest<TodoItemSearchFilter> { PageSize = 10, PageIndex = 1 };
        var response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        search = new SearchRequest<TodoItemSearchFilter> { PageSize = 2, PageIndex = 1 };
        response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        search = new SearchRequest<TodoItemSearchFilter> { PageSize = 3, PageIndex = 2 };
        response = await repoQuery.SearchTodoItemAsync(search);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetPageEntitiesAsync_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.Add(TodoDbContextSupport.TodoItemFactory("some entity a")))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        var response = await repoQuery.QueryPageAsync<TodoItem>(pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.QueryPageAsync<TodoItem>(pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.QueryPageAsync<TodoItem>(pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetPageProjectionAsync_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.Add(TodoDbContextSupport.TodoItemFactory("some entity a")))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        var response = await repoQuery.QueryPageProjectionAsync(TodoItemMapper.Projector, pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.QueryPageProjectionAsync(TodoItemMapper.Projector, pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.QueryPageProjectionAsync(TodoItemMapper.Projector, pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetStream_and_batch_concurrent_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.AddRange(TodoDbContextSupport.TodoItemListFactory(10)))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        using var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var batchsize = 3;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ConcurrentBatchAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            await Task.Delay(10);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, batchsize, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(total > 0);
    }

    [TestMethod]
    public async Task GetStream_and_pipe_concurrent_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.AddRange(TodoDbContextSupport.TodoItemListFactory(100)))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        using var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var maxConcurrent = 3;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ConcurrentPipeAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            await Task.Delay(10);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxConcurrent, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(total > 0);
    }

    [TestMethod]
    public async Task GetStream_process_parallel_async_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.AddRange(TodoDbContextSupport.TodoItemListFactory(100)))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        using var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var maxDegreesOfParallelism = -1;  //Environment.ProcessorCount;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ProcessParallelAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            //some async work
            await Task.Delay(10);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxDegreesOfParallelism, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(total > 0);
    }

    [TestMethod]
    public async Task GetStream_process_parallel_sync_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.AddRange(TodoDbContextSupport.TodoItemListFactory(10)))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        using var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var maxDegreesOfParallelism = -1; //Environment.ProcessorCount;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ProcessParallelSync((item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            //some sync work
            for (int i = 0; i < 1000; i++)
            {
                _ = i * 2;
            }
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxDegreesOfParallelism, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} Elapsed MS:{elapsed_time}");
        Assert.IsTrue(total > 0);
    }

    [TestMethod]
    public async Task SearchWithFilterAndSort_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.AddRange(
                [
                    TodoDbContextSupport.TodoItemFactory("A some entity a", TodoItemStatus.InProgress),
                    TodoDbContextSupport.TodoItemFactory("B some entity a", TodoItemStatus.InProgress),
                    TodoDbContextSupport.TodoItemFactory("C some entity a", TodoItemStatus.InProgress)
                ]);
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //search criteria
        var search = new SearchRequest<TodoItemSearchFilter>
        {
            PageSize = 10,
            PageIndex = 1,
            Filter = new TodoItemSearchFilter { Statuses = [TodoItemStatus.InProgress] },
            Sorts = [new("Name", SortOrder.Descending)]
        };

        //act
        var response = await repoQuery.SearchTodoItemAsync(search);

        //assert
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
    }

    [TestMethod]
    public async Task ProcessResultSetConcurrent_pass()
    {
        //arrange

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.Add(TodoDbContextSupport.TodoItemFactory("some entity a")))
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src);

        //act & assert
        var response = await repoQuery.QueryPageProjectionAsync(TodoItemMapper.Projector, pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        //concurrent processing example should run multiple threads concurrently, take about 1 sec total
        var cBag = new ConcurrentBag<Guid>();
        var tasks = response.Data.Select(async t =>
        {
            //some awaitable task; not EF DbContext which not thread-safe and can only handle one operation at a time
            await Task.Delay(1000);
            cBag.Add((Guid)t.Id!);
        });
        await Task.WhenAll(tasks);
        Assert.AreEqual(4, cBag.Count);
    }

}
