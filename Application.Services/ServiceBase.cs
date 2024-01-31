namespace Application.Services;

public abstract class ServiceBase(ILogger<ServiceBase> logger)
{
    protected void SomeCommonMethod()
    {
        _ = logger;
    }
}
