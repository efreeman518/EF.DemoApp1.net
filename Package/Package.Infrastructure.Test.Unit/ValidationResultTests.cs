using Package.Infrastructure.Common;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class ValidationResultTests
{
    [DataTestMethod]
    [DataRow(true, null)]
    [DataRow(false, null)]
    [DataRow(true, "true message")]
    [DataRow(false, "false message")]
    public void ValidationResponse_pass(bool valid, string message)
    {
        var vr = new ValidationResult(valid, [message]);
        Assert.IsNotNull(vr);
        Assert.AreEqual(valid, vr.IsValid);
        Assert.IsTrue(message == vr.Messages.FirstOrDefault());

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
}