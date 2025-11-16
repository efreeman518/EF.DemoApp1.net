namespace SampleApp.Bootstrapper;

public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
