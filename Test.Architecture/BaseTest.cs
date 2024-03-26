using System.Reflection;

namespace Test.Architecture;
public abstract class BaseTest
{
    protected static readonly Assembly DomainModelAssembly = typeof(Domain.Model.TodoItem).Assembly;
    protected static readonly Assembly DomainRulesAssembly = typeof(Domain.Rules.TodoCompositeRule).Assembly;
    protected static readonly Assembly DomainSharedAssembly = typeof(Domain.Shared.Constants.Constants).Assembly;
    protected static readonly Assembly ApplicationServicesAssembly = typeof(Application.Services.TodoService).Assembly;
    protected static readonly Assembly SampleAppApiAssembly = typeof(SampleApp.Api.Controllers.TodoItemsController).Assembly;
}
