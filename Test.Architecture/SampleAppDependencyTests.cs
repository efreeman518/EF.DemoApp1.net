using NetArchTest.Rules;

namespace Test.Architecture;

[TestClass]
public class SampleAppDependencyTests : BaseTest
{
    [TestMethod]
    public void SampleAppApi_Dependency_Check()
    {
        string[] notAllowedDependencies = ["Domain", "Package.Infrastructure.Data", "EntityFrameworkCore"];

        //domain projects should not have dependencies on other projects
        var result = Types.InAssembly(SampleAppApiAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(notAllowedDependencies)
            .GetResult();

        //assert
        Assert.IsTrue(result.IsSuccessful, result.FailingTypeNames != null ? string.Join(", ", result.FailingTypeNames) : null);
    }

}