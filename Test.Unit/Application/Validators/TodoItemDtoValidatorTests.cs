using Application.Contracts.Model;
using Domain.Shared.Enums;
using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleApp.Support.Validators;

namespace Test.Unit.Application.Validators;

[TestClass]
public class TodoItemDtoValidatorTests : UnitTestBase
{
    public TodoItemDtoValidatorTests()
    {
    }

    [DataTestMethod]
    [DataRow("", false)]
    [DataRow("a", false)]
    [DataRow("sdfg", false)]
    [DataRow("sdfgsd456yrt", false)]
    [DataRow("some entity a", true)]
    [DataRow("sdfga", true)]
    public async Task Validation_Check_pass(string name, bool expectedValid)
    {
        var validator = new TodoItemDtoValidator();
        var item = new TodoItemDto(null, name, TodoItemStatus.Created);
        var result = await validator.TestValidateAsync(item);
        bool isValid = result.IsValid;
        Assert.AreEqual(expectedValid, isValid);
    }
}
