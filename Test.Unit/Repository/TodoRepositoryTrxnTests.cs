using Domain.Model;
using Domain.Shared.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Data.Contracts;
using System;
using System.Threading.Tasks;
using Test.Support;

namespace Test.Unit.Repository;

[TestClass]
public class TodoRepositoryTrxnTests : UnitTestBase
{
    public TodoRepositoryTrxnTests() : base()
    {
    }

    [TestMethod]
    public async Task CRUD_pass()
    {
        //arrange

        //InMemory setup & seed
        var dbTrxn = new InMemoryDbBuilder().Build<TodoDbContextTrxn>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryTrxn repoTrxn = new TodoRepositoryTrxn(dbTrxn, audit);
        var todo = new TodoItem("wash car");

        //act & assert

        //create
        Assert.IsTrue(todo.Id != Guid.Empty);
        repoTrxn.Create(ref todo);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.Throw);
        var id = todo.Id;
        Assert.IsTrue(id != Guid.Empty);

        //retrieve
        todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id);
        Assert.AreEqual(id, todo!.Id);

        //update
        string newName = "mow lawn";
        todo.SetStatus(TodoItemStatus.Completed);
        todo.SetName(newName);
        repoTrxn.UpdateFull(ref todo); //update full record;
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.Throw);
        Assert.IsTrue(todo?.IsComplete);
        Assert.AreEqual(newName, todo?.Name);

        //delete
        await repoTrxn.DeleteAsync<TodoItem>(id);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.Throw);

        todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id);
        Assert.IsNull(todo);
    }

}
