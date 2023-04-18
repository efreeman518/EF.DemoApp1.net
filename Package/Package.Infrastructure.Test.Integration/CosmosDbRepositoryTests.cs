using Domain.Model;
using Domain.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.CosmosDb;

namespace Package.Infrastructure.Test.Integration;

/// <summary>
/// CosmosDb emulator: https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21
/// </summary>
//[Ignore("CosmosDb emulator needs to be running.")]
[TestClass]
public class CosmosDbRepositoryTests : IntegrationTestBase
{
    readonly ILogger<CosmosDbRepositoryTests> _logger;
    readonly ICosmosDbRepository _repo;

    public CosmosDbRepositoryTests() : base()
    {
        _logger = LoggerFactory.CreateLogger<CosmosDbRepositoryTests>();
        _repo = (CosmosDbRepository)Services.GetRequiredService(typeof(CosmosDbRepository));
    }

    /// <summary>
    /// CosmosDb - create Db 'SampleDB' and containter '
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Todo_crud_pass()
    {
        _logger.Log(LogLevel.Information, "Todo_crud_pass - Start");

        TodoItemNoSql todo = new("testToDoItema");
        Guid id = todo.Id;

        await _repo.SaveItemAsync(todo);

        TodoItemNoSql? t2 = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNotNull(t2);

        List<TodoItemNoSql> todos = await _repo.GetListAsync<TodoItemNoSql>(t => t.Status == TodoItemStatus.Created);

        Assert.IsTrue(todos.Count > 0);

        todos = await _repo.GetListAsync<TodoItemNoSql>(t => t.Status == TodoItemStatus.Created);

        //todos = await _repo.GetListAsync<TodoItemNoSql>("Select * FROM Todo t Where t.Status = 1");

        Assert.IsTrue(todos.Count > 0);

        await _repo.DeleteItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);

        t2 = await _repo.GetItemAsync<TodoItemNoSql>(id.ToString(), id.ToString()[..5]);
        Assert.IsNull(t2);

    }
}
