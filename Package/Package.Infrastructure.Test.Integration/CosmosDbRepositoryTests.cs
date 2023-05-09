using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.CosmosDb;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.Test.Integration;

// CosmosDb emulator: https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21

[Ignore("CosmosDb emulator needs to be running, with connection string in settings and SampleDB created")]
[TestClass]
public class CosmosDbRepositoryTests : IntegrationTestBase
{
    readonly ILogger<CosmosDbRepositoryTests> _logger;
    readonly ICosmosDbRepository _repo;

    public CosmosDbRepositoryTests() : base()
    {
        _logger = LoggerFactory.CreateLogger<CosmosDbRepositoryTests>();
        _repo = (ICosmosDbRepository)Services.GetRequiredService(typeof(ICosmosDbRepository));
    }

    /// <summary>
    /// CosmosDb - create Db 'SampleDB' and container 'TodoItemNoSql'
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Todo_crud_pass()
    {
        _logger.Log(LogLevel.Information, "Todo_crud_pass - Start");

        TodoItemNoSql? todo = new(Guid.NewGuid().ToString() + "a"); //EntityBase - Id created on instantiation 
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

        //LINQ - page with filter and sort

        //filter
        Expression<Func<TodoItemNoSql, bool>> filter = t => t.Status == TodoItemStatus.Completed;
        //sort
        List<Sort> sorts = new() { new Sort("PartitionKey", SortOrder.Ascending) };
        //page size
        int pageSize = 10;

        List<TodoItemNoSql> todos;
        string? continuationToken = null;
        do
        {
            (todos, continuationToken) = await _repo.GetPagedListAsync(continuationToken, pageSize, filter, sorts);
            Assert.IsTrue(todos.Count > 0);
        }
        while (continuationToken != null);

        //SQL - page with filter and sort
        string sql = "SELECT * FROM t WHERE t.Status=@Status ORDER BY t.Name ASC";
        Dictionary<string, object> parameters = new()
        {
            {"@Status", TodoItemStatus.Completed }
        };
        do
        {
            (todos, continuationToken) = await _repo.GetPagedListAsync<TodoItemNoSql>(sql, parameters, continuationToken, pageSize);
            Assert.IsTrue(todos.Count > 0);
        }
        while (continuationToken != null);

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
