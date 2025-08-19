using Domain.Model;
using Domain.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Unit.DomainRules;

[TestClass]
public class TodoItemRulesTests
{
    [TestMethod]
    [DataRow("asdfg", 6, false)]
    [DataRow("asdfg", 5, true)]
    [DataRow("asdfgsd456yrt", 5, true)]
    [DataRow("asdfgsd456yrt", 20, false)]
    public void NameLengthRule_ReturnsExpected(string name, int nameLength, bool expectedValid)
    {
        var item = new TodoItem(name);
        bool isValid = new TodoNameLengthRule(nameLength).IsSatisfiedBy(item);
        Assert.AreEqual(expectedValid, isValid);
    }

    [TestMethod]
    [DataRow("axyzsdfghgfd", "xyz")]
    [DataRow("asdfgxyzhgfd", "xyz")]
    [DataRow("axyzsdfghxyz", "xyz")]
    public void NameContentRule_pass(string name, string contains)
    {
        var item = new TodoItem(name);
        bool isValid = new TodoNameRegexRule(contains).IsSatisfiedBy(item);
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    [DataRow("axyzgh", 5, "xyz")]
    [DataRow("axyzsdfghxyz", 5, "xyz")]
    public void CompositeRule_pass(string name, int nameLength, string contains)
    {
        var item = new TodoItem(name);
        bool isValid = new TodoCompositeRule(nameLength, contains).IsSatisfiedBy(item);
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    [DataRow("aaaaa", 6, "a")]
    [DataRow("axyzsdfg", 10, "xyz")]
    [DataRow("asgfdgr", 5, "xyz")]
    public void CompositeRule_fail(string name, int nameLength, string contains)
    {
        var item = new TodoItem(name);
        bool isValid = new TodoCompositeRule(nameLength, contains).IsSatisfiedBy(item);
        Assert.IsFalse(isValid);
    }
}
