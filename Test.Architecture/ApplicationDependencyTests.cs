using NetArchTest.Rules;

namespace Test.Architecture;

[TestClass]
public class ApplicationDependencyTests : BaseTest
{
    [TestMethod]
    public void ApplicationServices_Dependency_Check()
    {
        string[] notAllowedDependencies = ["SampleApp", "EntityFrameworkCore"];

        //domain projects should not have dependencies on other projects
        var result = Types.InAssembly(DomainModelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(notAllowedDependencies)
            .GetResult();

        //assert
        Assert.IsTrue(result.IsSuccessful, result.FailingTypeNames != null ? string.Join(", ", result.FailingTypeNames) : null);
    }

}