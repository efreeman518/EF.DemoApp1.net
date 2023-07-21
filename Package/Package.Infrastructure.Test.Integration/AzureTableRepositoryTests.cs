using Domain.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Package.Infrastructure.Table;
using Package.Infrastructure.Test.Integration.Model;
using System.Linq.Expressions;
using System.Net;

namespace Package.Infrastructure.Test.Integration;

//[Ignore("Table account required - Azurite storage emulator, Azure Storage, CosmosDB emulator or CosomsDB.")]

[TestClass]
public class AzureTableRepositoryTests : IntegrationTestBase
{
    readonly ITableRepository _repo;

    public AzureTableRepositoryTests() : base()
    {
        _repo = (ITableRepository)Services.GetRequiredService(typeof(ITableRepository));
    }

    [TestMethod]
    public async Task Todo_crud_pass()
    {
        var tableServiceClientName = "AzureTable1"; //from service registration .AddTableServiceClient(configSection).WithName("AzureTable1");

        //Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableServiceClientName, tableName);

        //Create entity
        TodoItemTableEntity? todo = new(name:"item1-a"); 

        //create
        var responseCode = await _repo.CreateItemAsync(tableServiceClientName, todo);
        Assert.AreEqual(HttpStatusCode.NoContent, responseCode);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemTableEntity>(tableServiceClientName, todo.PartitionKey, todo.RowKey);
        Assert.IsNotNull(todo);
        Assert.AreEqual(TodoItemStatus.Created, todo.Status);

        //update
        todo.SetStatus(TodoItemStatus.Completed);
        responseCode = await _repo.UpdateItemAsync(tableServiceClientName, todo, TableUpdateMode.Replace);
        Assert.AreEqual(HttpStatusCode.NoContent, responseCode);

        //retrieve & validate 
        todo = await _repo.GetItemAsync<TodoItemTableEntity>(tableServiceClientName, todo.PartitionKey, todo.RowKey);
        Assert.IsNotNull(todo);
        Assert.AreEqual(TodoItemStatus.Completed, todo.Status);

        //delete & validate
        await _repo.DeleteItemAsync<TodoItemTableEntity>(tableServiceClientName, todo.PartitionKey, todo.RowKey);
        todo = await _repo.GetItemAsync<TodoItemTableEntity>(tableServiceClientName, todo.PartitionKey, todo.RowKey);
        Assert.IsNull(todo);

        //delete table
        responseCode = await _repo.DeleteTableAsync(tableServiceClientName, tableName);
        Assert.AreEqual(HttpStatusCode.NoContent, responseCode);
    }

    [TestMethod]
    public async Task Populate_table_run_page_queries_pass()
    {
        var tableServiceClientName = "AzureTable1"; //from service registration .AddTableServiceClient(configSection).WithName("AzureTable1");

        //Create Table
        var tableName = nameof(TodoItemTableEntity);
        _ = await _repo.GetOrCreateTableAsync(tableServiceClientName, tableName);

        //populate table with docs (random Status)
        Array statuses = Enum.GetValues(typeof(TodoItemStatus));
        Random random = new();
        for (var i = 0; i < 100; i++)
        {
            TodoItemStatus status = (TodoItemStatus)(statuses.GetValue(random.Next(statuses.Length)) ?? TodoItemStatus.Created);
            TodoItemTableEntity todo1 = new(status: status); //TableEntity - PartitionKey, RowKey created on instantiation 
            await _repo.CreateItemAsync(tableServiceClientName, todo1);
        }

        List<TodoItemTableEntity> fullList = new List<TodoItemTableEntity>();
        IReadOnlyList<TodoItemTableEntity>? todos;
        int pageSize = 10;

        //LINQ - page with filter
        //filterLinq = t => t.IsComplete;
        //filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString()); //Table SDK filter by enum is ugly atm
        Expression<Func<TodoItemTableEntity, bool>> filterLinq = t => t.Status.Equals(TodoItemStatus.Completed.ToString());
        bool includeTotal = true;
        int total = -1;
        int total1;
        string? continuationToken = null;
        
        do
        {
            (todos, total1, continuationToken) = await _repo.QueryAsync(tableServiceClientName, continuationToken, pageSize, filterLinq, includeTotal: includeTotal);
            if (includeTotal)
            {
                total = total1;
                Assert.IsTrue(total > -1);
            }
            Assert.IsTrue(todos?.Count > 0);
            fullList.AddRange(todos);
            includeTotal = false; //only retrieve the first time
        }
        while (continuationToken != null);

        Assert.AreEqual(total,fullList.Count);

        //OData - page with filter
        fullList.Clear();

        //filterOData = "IsComplete eq true";
        string filterOData = "Status eq 'Completed'";
        includeTotal = true;
        total = 0;
        continuationToken = null;

        do
        {
            (todos, total1, continuationToken) = await _repo.QueryAsync<TodoItemTableEntity>(tableServiceClientName, continuationToken, pageSize, null, filterOData, includeTotal: includeTotal);
            if (includeTotal)
            {
                total = total1;
                Assert.IsTrue(total > -1);
            }
            Assert.IsTrue(todos?.Count > 0);
            fullList.AddRange(todos);
            includeTotal = false;
        }
        while (continuationToken != null);

        Assert.AreEqual(total, fullList.Count);
    }
}


