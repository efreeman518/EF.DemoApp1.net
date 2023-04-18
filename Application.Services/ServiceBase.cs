namespace Application.Services;

public abstract class ServiceBase
{
    protected readonly ILogger<ServiceBase> Logger;

    protected ServiceBase(ILogger<ServiceBase> logger)
    {
        Logger = logger;
    }
}
