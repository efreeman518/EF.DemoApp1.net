namespace SampleApp.Bootstrapper;
public interface IStartupTask
{
    Task Execute(CancellationToken cancellationToken = default);
}
