using Application.Contracts.Model;
using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, rc, _mapper);

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
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, src, _mapper);

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
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        var response = await repoQuery.GetPageEntitiesAsync<TodoItem>(pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.GetPageEntitiesAsync<TodoItem>(pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.GetPageEntitiesAsync<TodoItem>(pageSize: 3, pageIndex: 2, includeTotal: true);
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
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        var response = await repoQuery.GetPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.GetPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.GetPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetStreamEntitiesAsync_pass()
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
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        var cancellationTokenSource = new CancellationTokenSource();
        Debug.WriteLine($"{DateTime.UtcNow} - Start");
        var i = 0;
        var stream = repoQuery.GetStream<TodoItem>().WithCancellation(cancellationTokenSource.Token);
        await foreach (var item in stream)
        {
            //sync or await some async processing on the item
            Debug.WriteLine($"{DateTime.UtcNow} - {++i} - {item.Name}");
        }
        Debug.WriteLine($"{DateTime.UtcNow} - Finish");
        Assert.AreEqual(4, i);
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
                    new TodoItem ("A some entity a", TodoItemStatus.InProgress),
                    new TodoItem ("B some entity a", TodoItemStatus.InProgress),
                    new TodoItem ("C some entity a", TodoItemStatus.InProgress)
                });
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .BuildInMemory<TodoDbContextQuery>();

        var src = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //search criteria
        var search = new SearchRequest<TodoItemSearchFilter>
        {
            PageSize = 10,
            PageIndex = 1,
            Filter = new TodoItemSearchFilter { Statuses = new List<TodoItemStatus> { TodoItemStatus.InProgress } },
            Sorts = new List<Sort> { new Sort("Name", SortOrder.Descending) }
        };

        //act
        var response = await repoQuery.SearchTodoItemAsync(search);

        //assert
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        var indexOfA = response.Data.FindIndex(e => e.Name.StartsWith("A"));
        var indexOfB = response.Data.FindIndex(e => e.Name.StartsWith("B"));
        var indexOfC = response.Data.FindIndex(e => e.Name.StartsWith("C"));
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
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, src, _mapper);

        //act & assert
        var response = await repoQuery.GetPageProjectionAsync<TodoItem, TodoItemDto>(_mapper.ConfigurationProvider, pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);


        //concurrent processing example should run multiple threads concurrently, take about 1 sec total
        var cBag = new ConcurrentBag<Guid>();
        var tasks = response.Data.Select(async t =>
        {
            //some awaitable task; not EF DbContext which can only handle one at a time
            await Task.Delay(1000);
            cBag.Add(t.Id);
        });
        await Task.WhenAll(tasks);
        Assert.AreEqual(4, cBag.Count);
    }
}
