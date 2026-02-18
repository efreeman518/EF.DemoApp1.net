using Package.Infrastructure.Common;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class ValidationResultTests
{
    [TestMethod]
    [DataRow(true, null)]
    [DataRow(false, null)]
    [DataRow(true, "true message")]
    [DataRow(false, "false message")]
    public void ValidationResponse_pass(bool valid, string message)
    {
        var vr = new ValidationResult(valid, [message]);
        Assert.IsNotNull(vr);
        Assert.AreEqual(valid, vr.IsValid);
        Assert.AreEqual(vr.Messages.FirstOrDefault(), message);

        var vr1 = vr;
        Assert.IsTrue(vr.Equals(vr1)); //ReferenceEquals

        vr1 = (ValidationResult)valid; //explicit operator requires a cast
        Assert.AreEqual(valid, vr1); //implicit operator 

        Assert.AreEqual(vr.IsValid, vr1.IsValid);
        Assert.IsTrue(vr.Equals(vr1)); //bool Equals(ValidationResponse? vr)
        Assert.IsTrue(vr.Equals((object)vr1)); // override bool Equals(object? obj)

        Assert.IsTrue(vr == vr1); //operator ==
        vr1.IsValid = !vr.IsValid;
        Assert.IsTrue(vr != vr1); //operator !=

    }

    [TestMethod]
    public void ValidationResult_StaticEquals_handles_null_cases()
    {
        ValidationResult? left = null;
        ValidationResult? right = null;
        Assert.IsTrue(ValidationResult.Equals(left, right));

        right = ValidationResult.True();
        Assert.IsFalse(ValidationResult.Equals(left, right));
        Assert.IsFalse(ValidationResult.Equals(right, left));
    }

    [TestMethod]
    public void ValidationResult_operators_match_static_comparer_semantics()
    {
        ValidationResult? left = null;
        ValidationResult? right = null;
        Assert.IsTrue(left == right);

        left = ValidationResult.True(["a"]);
        right = ValidationResult.True(["b"]);
        Assert.IsTrue(left == right); // equality ignores messages

        right = ValidationResult.False();
        Assert.IsTrue(left != right);
    }
}