using Application.Contracts.Model;
using Application.Services.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Unit.Application.Rules;

[TestClass]
public class TodoItemDtoRulesTests
{
    [DataTestMethod]
    [DataRow("sdfg", 5, false)]
    [DataRow("sdfg", 3, true)]
    [DataRow("sdfgsd456yrt", 5, true)]
    [DataRow("sdfgsd456yrt", 20, false)]
    public void NameLengthRule_pass(string name, int nameLength, bool expectedValid)
    {
        var item = new TodoItemDto { Name = name };
        bool isValid = new TodoNameLengthRule(nameLength).IsSatisfiedBy(item);
        Assert.AreEqual(expectedValid, isValid);
    }

    [DataTestMethod]
    [DataRow("sdfg", "xyz", false)]
    [DataRow("xyzsdfghgfd", "xyz", true)]
    [DataRow("sdfgxyzhgfd", "xyz", true)]
    [DataRow("xyzsdfghxyz", "xyz", true)]
    public void NameContentRule_pass(string name, string contains, bool expectedValid)
    {
        var item = new TodoItemDto { Name = name };
        bool isValid = new TodoNameRegexRule(contains).IsSatisfiedBy(item);
        Assert.AreEqual(expectedValid, isValid);
    }

    [DataTestMethod]
    [DataRow("sdfg", 5, "xyz", false)]
    [DataRow("xyzsdfg", 10, "xyz", false)]
    [DataRow("xyzgh", 5, "xyz", true)]
    [DataRow("sdfghgfd", 5, "xyz", false)]
    [DataRow("xyzsdfghxyz", 5, "xyz", true)]
    public void CompositeRule_pass(string name, int nameLength, string contains, bool expectedValid)
    {
        var item = new TodoItemDto { Name = name };
        bool isValid = new TodoCompositeRule(nameLength, contains).IsSatisfiedBy(item);
        Assert.AreEqual(expectedValid, isValid);
    }
}
