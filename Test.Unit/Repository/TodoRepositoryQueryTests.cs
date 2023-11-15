using Application.Contracts.Model;
using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Data.Contracts;
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

        //custom data scenario that default seed data does not cover
        static void customData(List<TodoItem> entities)
        {
            entities.Add(new TodoItem("custom entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var rc = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, rc, _mapper);

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

        //custom data scenario that default seed data does not cover
        static void customData(List<TodoItem> entities)
        {
            entities.Add(new TodoItem("custom entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildSQLite<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

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
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

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
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        var response = await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetStream_and_batch_concurrent_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a1"));
            entities.Add(new TodoItem("some entity a2"));
            entities.Add(new TodoItem("some entity a3"));
            entities.Add(new TodoItem("some entity a4"));
            entities.Add(new TodoItem("some entity a5"));
            entities.Add(new TodoItem("some entity a6"));
            entities.Add(new TodoItem("some entity a7"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var batchsize = 3;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ConcurrentBatchAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            await Task.Delay(1000);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, batchsize, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task GetStream_and_pipe_concurrent_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a1"));
            entities.Add(new TodoItem("some entity a2"));
            entities.Add(new TodoItem("some entity a3"));
            entities.Add(new TodoItem("some entity a4"));
            entities.Add(new TodoItem("some entity a5"));
            entities.Add(new TodoItem("some entity a6"));
            entities.Add(new TodoItem("some entity a7"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var maxConcurrent = 3;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ConcurrentPipeAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            await Task.Delay(1000);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxConcurrent, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task GetStream_process_parallel_async_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a1"));
            entities.Add(new TodoItem("some entity a2"));
            entities.Add(new TodoItem("some entity a3"));
            entities.Add(new TodoItem("some entity a4"));
            entities.Add(new TodoItem("some entity a5"));
            entities.Add(new TodoItem("some entity a6"));
            entities.Add(new TodoItem("some entity a7"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var maxDegreesOfParallelism = -1;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ProcessParallelAsync(async (item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            //some async work
            await Task.Delay(1000);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxDegreesOfParallelism, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task GetStream_process_parallel_sync_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a1"));
            entities.Add(new TodoItem("some entity a2"));
            entities.Add(new TodoItem("some entity a3"));
            entities.Add(new TodoItem("some entity a4"));
            entities.Add(new TodoItem("some entity a5"));
            entities.Add(new TodoItem("some entity a6"));
            entities.Add(new TodoItem("some entity a7"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        Debug.WriteLine($"{DateTime.Now} - Start");

        var cancellationTokenSource = new CancellationTokenSource();
        var stream = repoQuery.GetStream<TodoItem>();
        var maxDegreesOfParallelism = -1;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var total = await stream.ProcessParallelSync((item) =>
        {
            Debug.WriteLine($"{DateTime.Now} {item.Name} start.");
            //some sync work
            Task.Delay(1000);
            Debug.WriteLine($"{DateTime.Now} {item.Name} finish.");
        }, maxDegreesOfParallelism, cancellationTokenSource.Token);

        stopwatch.Stop();
        var elapsed_time = stopwatch.ElapsedMilliseconds;

        Debug.WriteLine($"{DateTime.Now} - Finish Total:{total} ElapsedMS:{elapsed_time}");
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task SearchWithFilterAndSort_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.AddRange(new List<TodoItem>
                {
                    new("A some entity a", TodoItemStatus.InProgress),
                    new("B some entity a", TodoItemStatus.InProgress),
                    new("C some entity a", TodoItemStatus.InProgress)
                });
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

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
        var indexOfA = response.Data.FindIndex(e => e.Name.StartsWith('A'));
        var indexOfB = response.Data.FindIndex(e => e.Name.StartsWith('B'));
        var indexOfC = response.Data.FindIndex(e => e.Name.StartsWith('C'));
        Assert.IsTrue(indexOfC < indexOfB);
        Assert.IsTrue(indexOfB < indexOfA);
    }

    [TestMethod]
    public async Task ProcessResultSetConcurrent_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem("some entity a"));
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        var repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        var response = await repoQuery.QueryPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        //concurrent processing example should run multiple threads concurrently, take about 1 sec total
        var cBag = new ConcurrentBag<Guid>();
        var tasks = response.Data.Select(async t =>
        {
            //some awaitable task; not EF DbContext which not thread-safe and can only handle one operation at a time
            await Task.Delay(1000);
            cBag.Add(t.Id);
        });
        await Task.WhenAll(tasks);
        Assert.AreEqual(4, cBag.Count);
    }
}
