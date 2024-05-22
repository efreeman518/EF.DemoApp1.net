using Domain.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Package.Infrastructure.Common.Exceptions;

namespace Test.Unit.DomainModels;

[TestClass]
public class TodoItemTests
{
    [DataTestMethod]
    [DataRow("asdfg")]
    [DataRow("sdfgsd4a56yrt")]
    public void Validate_Success(string name)
    {
        var item = new TodoItem(name) { CreatedBy = "Test.Unit" };
        var response = item.Validate();
        Assert.IsNull(response);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("sdfg")]
    [DataRow("sdfgsd456yrt")]
    [DataRow("sdfhy56u7g")]
    [DataRow("aafg")]
    [ExpectedException(typeof(ValidationException))]
    public void Validate_Throws(string name)
    {
        var t = new TodoItem(name) { CreatedBy = "Test.Unit" };
        t.Validate(true);
    }
}
