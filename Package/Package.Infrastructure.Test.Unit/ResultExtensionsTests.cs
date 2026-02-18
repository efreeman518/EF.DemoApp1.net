using Package.Infrastructure.Common.Extensions;
using Package.Infrastructure.Domain.Contracts;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class ResultExtensionsTests
{
    [TestMethod]
    public void ToResult_generic_maps_none_to_none()
    {
        var domain = DomainResult<string>.None();

        var result = domain.ToResult();

        Assert.IsTrue(result.IsNone);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void ToResult_generic_maps_success_with_null_value_without_forcing_failure()
    {
        var domain = DomainResult<string?>.Success(null);

        var result = domain.ToResult();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void ToResult_generic_maps_failures_to_string_errors()
    {
        var domain = DomainResult<string>.Failure("oops");

        var result = domain.ToResult();

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(1, result.Errors.Count);
        Assert.AreEqual("oops", result.Errors[0]);
    }
}
