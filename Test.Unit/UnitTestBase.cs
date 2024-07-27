using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

//no shared or external dependencies, run methods max parallel
[assembly: Parallelize(Workers = 5, Scope = ExecutionScope.MethodLevel)]

namespace Test.Unit;
public abstract class UnitTestBase
{
    protected MockRepository _mockFactory;

    protected UnitTestBase()
    {
        //MockBehavior.Strict not useable with extension methods (Logger.Log)
        //MockBehavior.Default = Loose - not have to Setup all called methods in the mock
        _mockFactory = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Mock };
    }

}
