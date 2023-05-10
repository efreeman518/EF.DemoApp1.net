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

    [TestMethod]
    public async Task PopulateContainer()
    {
        //CosmosDB Database must already exist (SampleDB)

        //create container if not exist
        await _repo.GetOrAddContainer(typeof(TodoItemNoSql).Name, "/PartitionKey", true);

        //randomize status
        Array statuses = Enum.GetValues(typeof(TodoItemStatus));
        Random random = new();

        //populate cosmosDb container
        for (var i = 0; i < 100; i++)
        {
            TodoItemStatus status = (TodoItemStatus)(statuses.GetValue(random.Next(statuses.Length)) ?? TodoItemStatus.Created);
            TodoItemNoSql todo1 = new($"todo-a-{i}", status); //EntityBase - Id created on instantiation 
            await _repo.SaveItemAsync(todo1);
        }

        Assert.IsTrue(true);
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

        //LINQ - page projection with filter and sort

        //filter
        Expression<Func<TodoDto, bool>> filter = t => t.Status == TodoItemStatus.Completed;
        //sort
        List<Sort> sorts = new() { new Sort("Name", SortOrder.Ascending) };
        //page size
        int pageSize = 10;

        List<TodoDto> todos;
        string? continuationToken = null;
        do
        {
            (todos, continuationToken) = await _repo.GetPagedListAsync<TodoItemNoSql, TodoDto>(continuationToken, pageSize, filter, sorts);
            Assert.IsTrue(todos.Count > 0);
        }
        while (continuationToken != null);

        //SQL - page projection with filter and sort

        string sql = "SELECT t.Id, t.Name, t.Status FROM TodoItemNoSql t WHERE t.Status=@Status ORDER BY t.Name ASC";
        Dictionary<string, object> parameters = new()
        {
            {"@Status", TodoItemStatus.Completed }
        };
        do
        {
            (var dtos, continuationToken) = await _repo.GetPagedListAsync<TodoItemNoSql,TodoDto>(sql, parameters, continuationToken, pageSize);
            Assert.IsTrue(dtos.Count > 0);
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
