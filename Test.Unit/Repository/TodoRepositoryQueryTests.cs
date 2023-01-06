using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task GetDtosAsync_pass()
    {
        //arrange

        //custom data scenario that default seed data does not cover
        static void customData(List<TodoItem> entities)
        {
            entities.Add(new TodoItem { Id = Guid.NewGuid(), Name = "some entity", CreatedBy = "Test.Unit", UpdatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow });
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .GetOrBuild<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, audit, _mapper);

        //act & assert
        var response = await repoQuery.GetPageTodoItemDtoAsync(10, 1);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.GetPageTodoItemDtoAsync(2, 1);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.GetPageTodoItemDtoAsync(3, 2);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
    }

    [TestMethod]
    public async Task GetPageEntityAsync_pass()
    {
        //arrange
        static void customData(List<TodoItem> entities)
        {
            //custom data scenario that default seed data does not cover
            entities.Add(new TodoItem { Id = Guid.NewGuid(), Name = "some entity", CreatedBy = "Test.Unit", UpdatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow });
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .GetOrBuild<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, audit, _mapper);

        //act & assert
        var response = await repoQuery.GetPageEntityAsync<TodoItem>(pageSize: 10, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repoQuery.GetPageEntityAsync<TodoItem>(pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repoQuery.GetPageEntityAsync<TodoItem>(pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);
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
                    new TodoItem { Id = Guid.NewGuid(), Name = "A some entity", Status = TodoItemStatus.InProgress, CreatedBy = "Test.Unit", UpdatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow },
                    new TodoItem { Id = Guid.NewGuid(), Name = "B some entity", Status = TodoItemStatus.InProgress, CreatedBy = "Test.Unit", UpdatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow },
                    new TodoItem { Id = Guid.NewGuid(), Name = "C some entity", Status = TodoItemStatus.InProgress, CreatedBy = "Test.Unit", UpdatedBy = "Test.Unit", CreatedDate = DateTime.UtcNow },
                });
        }

        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(customData)
            .GetOrBuild<TodoDbContextQuery>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryQuery repoQuery = new TodoRepositoryQuery(db, audit, _mapper);

        //search criteria
        var search = new SearchRequest<TodoItem>
        {
            PageSize = 10,
            PageIndex = 1,
            FilterItem = new TodoItem { Status = TodoItemStatus.InProgress, CreatedBy = "Test.Unit" },
            Sorts = new List<Sort> { new Sort("Name", SortOrder.Descending) }
        };

        //act
        var response = await repoQuery.SearchAsync(search);

        //assert
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        var indexOfA = response.Data.FindIndex(e => e.Name.StartsWith("A"));
        var indexOfB = response.Data.FindIndex(e => e.Name.StartsWith("B"));
        var indexOfC = response.Data.FindIndex(e => e.Name.StartsWith("C"));
        Assert.IsTrue(indexOfC < indexOfB);
        Assert.IsTrue(indexOfB < indexOfA);
    }
}
