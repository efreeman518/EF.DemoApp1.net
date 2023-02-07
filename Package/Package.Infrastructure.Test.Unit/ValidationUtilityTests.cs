using Package.Infrastructure.Common;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class ValidationUtilityTests
{
    [DataTestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("xyz", false)]
    [DataRow(".com", false)]
    [DataRow("xyz.com", false)]
    [DataRow("x@.com", false)]
    [DataRow("@xyz.com", false)]
    [DataRow("abc@xyz.com", true)]
    public void IsValidEmail_returns_expected(string email, bool expectedValid)
    {
        Assert.AreEqual(expectedValid, ValidationUtility.IsValidEmail(email));

    }
}