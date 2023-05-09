using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.CosmosDb;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.Test.Integration;

// CosmosDb emulator: https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21


/// <summary>
/// Need implementation of abstract base class for tests
/// </summary>
public class CosmosDbRepo1 : CosmosDbRepositoryBase
{
    public CosmosDbRepo1(CosmosDbRepositorySettings settings) : base(settings)
    {
    }
}

//[Ignore("CosmosDb emulator needs to be running, with connection string in settings and SampleDB created")]
[TestClass]
public class CosmosDbRepositoryTests : IntegrationTestBase
{
    readonly ILogger<CosmosDbRepositoryTests> _logger;
    readonly ICosmosDbRepositoryBase _repo;

    public CosmosDbRepositoryTests() : base()
    {
        _logger = LoggerFactory.CreateLogger<CosmosDbRepositoryTests>();
        _repo = (CosmosDbRepo1)Services.GetRequiredService(typeof(CosmosDbRepo1));
    }

    /// <summary>
    /// CosmosDb - create Db 'SampleDB' and container 'TodoItemNoSql'
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Todo_crud_pass()
    {
        _logger.Log(LogLevel.Information, "Todo_crud_pass - Start");

        TodoItemNoSql? todo = new("testToDoItema"); //EntityBase - Id created on instantiation 
        Guid id = todo.Id;

        //create
        await _repo.SaveItemAsync(todo);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNotNull(todo);
        Assert.AreEqual(todo.Status, TodoItemStatus.Created);

        //update
        todo.SetStatus(TodoItemStatus.Completed);
        await _repo.SaveItemAsync(todo);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNotNull(todo);
        Assert.AreEqual(todo.Status, TodoItemStatus.Completed);

        //query by expression
        List<TodoItemNoSql> todos = await _repo.GetListAsync<TodoItemNoSql>(t => t.Status == TodoItemStatus.Completed);
        Assert.IsTrue(todos.Count > 0);

        //query by sql
        todos = await _repo.GetListAsync<TodoItemNoSql>("Select * FROM Todo t Where t.Status = 1");
        Assert.IsTrue(todos.Count > 0);

        //delete & validate
        await _repo.DeleteItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        todo = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNull(todo);
    }
}

public class TodoDto
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public TodoItemStatus Status { get; set; }
}
