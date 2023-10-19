using Application.Contracts.Model;
using Application.Services.Validators;
using Domain.Model;
using FluentValidation.TestHelper;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common;
using Test.Support;

namespace Test.Unit.Application.Rules;

[TestClass]
public class TodoItemDtoValidatorTests : UnitTestBase
{
    private readonly TodoRepositoryQuery _todoRepositoryQuery;

    //custom data scenario that default seed data does not cover
    static void CustomData(List<TodoItem> entities)
    {
        entities.Add(new TodoItem("custom entity a"));
    }

    public TodoItemDtoValidatorTests()
    {
        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(CustomData)
            .BuildInMemory<TodoDbContextQuery>();

        var rc = new RequestContext(Guid.NewGuid().ToString(), "Test.Unit");
        _todoRepositoryQuery = new TodoRepositoryQuery(db, rc, _mapper);
    }

    [DataTestMethod]
    [DataRow("", false)]
    [DataRow("a", false)]
    [DataRow("sdfg", false)]
    [DataRow("sdfgsd456yrt", false)]
    [DataRow("custom entity a", false)]
    [DataRow("sdfga", true)]
    public async Task Validation_Check_pass(string name, bool expectedValid)
    {
        var validator = new TodoItemDtoValidator(_todoRepositoryQuery);
        var item = new TodoItemDto { Name = name };
        var result = await validator.TestValidateAsync(item);
        bool isValid = result.IsValid;
        Assert.AreEqual(expectedValid, isValid);
    }

    [TestMethod]
    //[ExpectedException(typeof(FluentValidation.ValidationException))]
    public async Task Validation_Update_exception()
    {
        var validator = new TodoItemDtoValidator(_todoRepositoryQuery);
        var item = new TodoItemDto { Id = Guid.NewGuid(), Name = "custom entity a" }; //existing
        var result = await validator.TestValidateAsync(item);
        Assert.IsFalse(result.IsValid);
    }
}
