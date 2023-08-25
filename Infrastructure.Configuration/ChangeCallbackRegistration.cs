namespace Infrastructure.Configuration;

/// <summary>
/// Unused; possible for DatabaseConfiguration refresh
/// </summary>
public class ChangeCallbackRegistration : IDisposable
{
    private readonly Action<object?> _callback;
    private readonly object? _state;
    private readonly CancellationTokenRegistration _registration;

    public ChangeCallbackRegistration(Action<object?> callback, object? state, CancellationToken token)
    {
        _callback = callback;
        _state = state;
        _registration = token.Register(InvokeCallback);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _registration.Dispose();
    }

    private void InvokeCallback()
    {
        // Invoke the callback with the state object
        _callback(_state);
    }
}
