using Application.Contracts.Model;
using Application.Services.Validators;
using Domain.Shared.Enums;
using FluentValidation.TestHelper;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Contracts;
using Test.Support;

namespace Test.Unit.Application.Rules;

[TestClass]
public class TodoItemDtoValidatorTests : UnitTestBase
{
    private readonly TodoRepositoryQuery _todoRepositoryQuery;

    public TodoItemDtoValidatorTests()
    {
        //InMemory setup & seed
        TodoDbContextQuery db = new InMemoryDbBuilder()
            .SeedDefaultEntityData()
            .UseEntityData(entities => entities.Add(TodoDbContextSupport.TodoItemFactory("custom entity a")))
            .BuildInMemory<TodoDbContextQuery>();

        var rc = new RequestContext<string>(Guid.NewGuid().ToString(), "Test.Unit");
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
        var item = new TodoItemDto(null, name, TodoItemStatus.Created);
        var result = await validator.TestValidateAsync(item);
        bool isValid = result.IsValid;
        Assert.AreEqual(expectedValid, isValid);
    }

    [TestMethod]
    //[ExpectedException(typeof(FluentValidation.ValidationException))]
    public async Task Validation_Update_exception()
    {
        var validator = new TodoItemDtoValidator(_todoRepositoryQuery);
        var item = new TodoItemDto(Guid.NewGuid(), "custom entity a", TodoItemStatus.Created); //existing
        var result = await validator.TestValidateAsync(item);
        Assert.IsFalse(result.IsValid);
    }
}
