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
        var dbTrxn = new InMemoryDbBuilder().GetOrBuild<TodoDbContextTrxn>();

        var audit = new AuditDetail("Test.Unit");
        ITodoRepositoryTrxn repoTrxn = new TodoRepositoryTrxn(dbTrxn, audit);
        var todo = new TodoItem { Name = "wash car", IsComplete = false };

        //act & assert

        //create
        Assert.IsTrue(todo.Id == Guid.Empty);
        repoTrxn.Save(ref todo);
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.Throw);
        var id = todo.Id;
        Assert.IsTrue(id != Guid.Empty);

        //retrieve
        todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id);
        Assert.AreEqual(id, todo!.Id);

        //update
        bool isComplete = true;
        string newName = "mow lawn";
        todo.IsComplete = isComplete;
        todo.Name = newName;
        repoTrxn.UpdateFull(ref todo); //update full record;
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.Throw);
        Assert.AreEqual(isComplete, todo?.IsComplete);
        Assert.AreEqual(newName, todo?.Name);

        //delete
        repoTrxn.Delete(new TodoItem { Id = id });
        await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.Throw);

        todo = await repoTrxn.GetEntityAsync<TodoItem>(filter: t => t.Id == id);
        Assert.IsNull(todo);
    }

}
