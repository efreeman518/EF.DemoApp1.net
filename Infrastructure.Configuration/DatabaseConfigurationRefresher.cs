using Microsoft.Extensions.Primitives;

namespace Infrastructure.Configuration;

public class DatabaseConfigurationRefresher : IDatabaseConfigurationRefresher
{
    private readonly Timer? _timer;
    private CancellationTokenSource? _cancellationTokenSource;

    public DatabaseConfigurationRefresher(TimeSpan? refreshInterval)
    {
        if (refreshInterval != null)
        {
            _timer = new Timer(Change, null, TimeSpan.Zero, (TimeSpan)refreshInterval);
        }
    }

    /// <summary>
    /// Modifying the CancellationTokenSource calls the change event callback which triggers the reload config from DB
    /// </summary>
    /// <param name="state"></param>
    private void Change(object? state)
    {
        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// returns the ChangeToken
    /// </summary>
    /// <returns></returns>
    public IChangeToken StartWatch()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        return new CancellationChangeToken(_cancellationTokenSource.Token);
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _timer?.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~DatabaseConfigurationRefresher()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
