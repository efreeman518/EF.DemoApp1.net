using Domain.Model;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Threading.Tasks;
using Test.Support;

namespace Test.Unit.Repository;

[TestClass]
public class TodoRepositoryTests : UnitTestBase
{
    public TodoRepositoryTests() : base()
    {
    }

    
    [TestMethod]
    public async Task GetItemsPagedAsync_pass()
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

        //act & assert
        var response = await repo.GetPagedListAsync<TodoItem>(pageSize:10, pageIndex:1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);

        response = await repo.GetPagedListAsync<TodoItem>(pageSize: 2, pageIndex: 1, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(2, response.Data.Count);
        Assert.AreEqual(4, response.Total);

        response = await repo.GetPagedListAsync<TodoItem>(pageSize: 3, pageIndex: 2, includeTotal: true);
        Assert.IsNotNull(response);
        Assert.AreEqual(1, response.Data.Count);
        Assert.AreEqual(4, response.Total);
    }

    [TestMethod]
    public async Task GetDtosAsync_pass()
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

        //act & assert
        var response = await repo.GetDtosPagedAsync(10, 1);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(4, response.Data.Count);

        response = await repo.GetDtosPagedAsync(2, 1);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(2, response.Data.Count);

        response = await repo.GetDtosPagedAsync(3, 2);
        Assert.IsNotNull(response);
        Assert.AreEqual(4, response.Total);
        Assert.AreEqual(1, response.Data.Count);

    }
}
