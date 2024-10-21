using Domain.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Unit.DomainModels;

[TestClass]
public class TodoItemTests
{
    [DataTestMethod]
    [DataRow("asdfg", true)]
    [DataRow("sdfgsd4a56yrt", true)]
    [DataRow(null, false)]
    [DataRow("sdfg", false)]
    [DataRow("sdfgsd456yrt", false)]
    [DataRow("sdfhy56u7g", false)]
    [DataRow("aafg", false)]
    public void Validate_IsValid(string name, bool isValid)
    {
        var item = new TodoItem(name);
        var response = item.Validate();
        Assert.AreEqual(isValid, response);
    }
}
