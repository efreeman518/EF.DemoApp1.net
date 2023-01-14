using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Bootstrapper;
public interface IStartupTask
{
    Task Execute(CancellationToken cancellationToken = default);
}
