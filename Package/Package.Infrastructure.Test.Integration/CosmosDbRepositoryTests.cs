using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.CosmosDb;
using Package.Infrastructure.Data.Contracts;
using System.Linq.Expressions;

namespace Package.Infrastructure.Test.Integration;

//CosmosDb emulator: https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21

//[Ignore("CosmosDb or emulator connection string required.")]

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

    [TestMethod]
    public async Task Populate_db_container_run_page_queries_pass()
    {
        await _repo.SetOrCreateDatabaseAsync("SampleDB");

        //create container if not exist
        await _repo.GetOrAddContainerAsync(typeof(TodoItemNoSql).Name, "/PartitionKey");

        //randomize status
        Array statuses = Enum.GetValues(typeof(TodoItemStatus));
        Random random = new();

        //populate cosmosDb container with docs (random Status)
        for (var i = 0; i < 100; i++)
        {
            TodoItemStatus status = (TodoItemStatus)(statuses.GetValue(random.Next(statuses.Length)) ?? TodoItemStatus.Created);
            TodoItemNoSql todo1 = new($"todo-a-{i}", status); //EntityBase - Id created on instantiation 
            await _repo.SaveItemAsync(todo1);
        }

        //LINQ - page projection with filter and sort
        //filter
        Expression<Func<TodoDto, bool>> filter = t => t.Status == TodoItemStatus.Completed;
        //sort
        List<Sort> sorts = new() { new Sort("Name", SortOrder.Ascending) };
        //page size
        int pageSize = 10;
        //total
        bool includeTotal = true;

        List<TodoDto> todos;
        int total = 0;
        string? continuationToken = null;
        do
        {
            (todos, total, continuationToken) = await _repo.GetPagedListAsync<TodoItemNoSql, TodoDto>(continuationToken, pageSize, filter, sorts, includeTotal);
            Assert.IsTrue(todos.Count > 0);
            Assert.IsTrue(!includeTotal || total > 0);
            includeTotal = false; //retrieve once, not repeatedly
        }
        while (continuationToken != null);

        //SQL - page projection with filter and sort
        string sql = "SELECT t.Id, t.Name, t.Status FROM TodoItemNoSql t WHERE t.Status=@Status ORDER BY t.Name ASC";
        string? sqlCount = "SELECT VALUE COUNT(1) FROM TodoItemNoSql t WHERE t.Status=@Status";

        continuationToken = null;
        Dictionary<string, object> parameters = new()
        {
            {"@Status", TodoItemStatus.Completed }
        };
        do
        {
            (todos, total, continuationToken) = await _repo.GetPagedListAsync<TodoItemNoSql, TodoDto>(
                continuationToken, pageSize, sql, sqlCount, parameters);
            Assert.IsTrue(todos.Count > 0);
            Assert.IsTrue(sqlCount == null || total > 0);
            sqlCount = null; //retrieve once, not repeatedly
        }
        while (continuationToken != null);
    }

    /// <summary>
    /// CosmosDb - create Db 'SampleDB' and container 'TodoItemNoSql'
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Todo_crud_pass()
    {
        _logger.Log(LogLevel.Information, "Todo_crud_pass - Start");

        //Create CosmosDB Database
        var dbId = Guid.NewGuid().ToString();
        await _repo.SetOrCreateDatabaseAsync(dbId);
        //ensure container exists
        var containerName = nameof(TodoItemNoSql);
        await _repo.GetOrAddContainerAsync(containerName, "/PartitionKey");

        TodoItemNoSql? todo = new(Guid.NewGuid().ToString() + "a"); //EntityBase - Id created on instantiation 
        Guid id = todo.Id;

        //create
        await _repo.SaveItemAsync(todo);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNotNull(todo);
        Assert.AreEqual(TodoItemStatus.Created, todo.Status);

        //update
        todo.SetStatus(TodoItemStatus.Completed);
        await _repo.SaveItemAsync(todo);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNotNull(todo);
        Assert.AreEqual(TodoItemStatus.Completed, todo.Status);

        //delete & validate
        await _repo.DeleteItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        todo = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNull(todo);

        //delete container
        await _repo.DeleteContainerAsync(containerName);
        //delete database
        await _repo.DeleteDatabaseAsync(dbId);
    }
}

public class TodoDto
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public TodoItemStatus Status { get; set; }
}
