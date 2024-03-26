using NetArchTest.Rules;

namespace Test.Architecture;

[TestClass]
public class DomainDependencyTests : BaseTest
{
    [TestMethod]
    public void DomainModel_Dependency_Check()
    {
        string[] notAllowedDependencies = ["Application", "Infrastructure", "EntityFrameworkCore"];

        //domain projects should not have dependencies on other projects
        var result = Types.InAssembly(DomainModelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(notAllowedDependencies)
            .GetResult();

        //assert
        Assert.IsTrue(result.IsSuccessful, result.FailingTypeNames != null ? string.Join(", ", result.FailingTypeNames) : null);
    }

    [TestMethod]
    public void DomainShared_Dependency_Check()
    {
        string[] notAllowedDependencies = ["Domain.Rules", "Domain.Model", "Application", "Infrastructure", "EntityFrameworkCore"];

        //domain projects should not have dependencies on other projects
        var result = Types.InAssembly(DomainSharedAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(notAllowedDependencies)
            .GetResult();

        //assert
        Assert.IsTrue(result.IsSuccessful, result.FailingTypeNames != null ? string.Join(", ", result.FailingTypeNames) : null);
    }

    [TestMethod]
    public void DomainRules_Dependency_Check()
    {
        string[] notAllowedDependencies = ["Application", "Infrastructure", "EntityFrameworkCore"];

        //domain projects should not have dependencies on other projects
        var result = Types.InAssembly(DomainRulesAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(notAllowedDependencies)
            .GetResult();

        //assert
        Assert.IsTrue(result.IsSuccessful, result.FailingTypeNames != null ? string.Join(", ", result.FailingTypeNames) : null);
    }
}