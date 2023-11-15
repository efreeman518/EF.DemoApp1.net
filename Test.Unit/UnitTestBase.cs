using AutoMapper;
using Moq;
using SampleApp.Bootstrapper.Automapper;

namespace Test.Unit;

public abstract class UnitTestBase
{
    protected MockRepository _mockFactory;

    //Infrastructure
    protected IMapper _mapper;

    protected UnitTestBase()
    {
        //MockBehavior.Strict not useable with extension methods (Logger.Log)
        //MockBehavior.Default = Loose - not have to Setup all called methods in the mock
        _mockFactory = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Mock };

        _mapper = ConfigureAutomapper.CreateMapper(
            [
                new MappingProfile()
            ]);
    }

}
