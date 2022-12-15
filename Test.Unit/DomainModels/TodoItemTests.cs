using Domain.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Utility.Exceptions;

namespace Test.Unit.DomainModels;

[TestClass]
public class TodoItemTests
{
    [DataTestMethod]
    [DataRow("sdfg", false)]
    [DataRow("asdfg", true)]
    [DataRow("sdfgsd4a56yrt", true)]
    [DataRow("sdfgsd456yrt", false)]
    public void Validate_returns_expected(string name, bool expectedValid)
    {
        var item = new TodoItem { Name = name };
        var response = item.Validate();
        Assert.AreEqual(expectedValid, response.IsValid);
    }

    [DataTestMethod]
    [DataRow("sdfhy56u7g")]
    [DataRow("aafg")]
    [ExpectedException(typeof(ValidationException))]
    public void Validate_Throws(string name)
    {
        var item = new TodoItem { Name = name };
        item.Validate(true);
    }
}
