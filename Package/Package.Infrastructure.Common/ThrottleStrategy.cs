using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace Application.Services;

/// <summary>
/// Centralized, process-wide rate limiting helper built on <see cref="System.Threading.RateLimiting"/>.
/// It ensures no more than a certain number of operations are allowed over time and provides
/// queueing behavior when limits are reached.
///
/// Key characteristics:
/// - Keys are case-insensitive. All callers using the same key share the same limiter instance (per-process).
/// - Limiters are stored and reused per key, and are disposed when replaced or removed.
/// - Thread-safe for concurrent configuration and execution.
///
/// Common scenarios supported:
/// - Pre-configure limiters per key (FixedWindow, SlidingWindow, TokenBucket, Concurrency) and reuse them without
///   passing options on every call.
/// - Execute with a pre-configured limiter (Task or Task&lt;T&gt; overloads).
/// - Provide a <c>waitTimeout</c> that caps how long to wait for a permit (separate from the action's cancellation).
/// - "Try" variants that return a boolean/tuple instead of throwing when queue is full or timed out.
/// - Introspection utilities (<c>IsConfigured</c>/<c>Remove</c>/<c>Clear</c>).
/// - Optional default factory for on-demand limiter creation when a key is not explicitly configured.
///
/// Notes:
/// - The first call to the parameterized <see cref="ExecuteAsync{T}(string, int, TimeSpan, int, Func{CancellationToken, Task{T}}, CancellationToken)"/>
///   establishes a FixedWindow limiter for that key if one doesn't exist yet. Subsequent calls with different
///   parameters WILL NOT reconfigure the existing limiter. To change limits at runtime, call one of the
///   <c>Configure*</c> methods (or <c>Remove</c>+recreate).
/// - <c>waitTimeout</c> applies only to waiting for a permit acquisition. It does not cancel the action.
///   Use the provided <c>cancellationToken</c> to cancel the action itself.
/// - If the limiter queue is full, acquisition fails immediately (no waiting). If a <c>waitTimeout</c> is specified,
///   an elapsed timeout also results in acquisition failure.
/// </summary>
public static class ThrottleStrategy
{
    private static readonly ConcurrentDictionary<string, RateLimiter> _limiters = new(StringComparer.OrdinalIgnoreCase);

    // Optional default factory used when ExecuteAsync is called with an unknown key.
    // If null, unknown keys will throw InvalidOperationException.
    private static Func<string, RateLimiter>? _defaultFactory;

    /// <summary>
    /// Executes the given async action under a fixed-window rate limit identified by the provided key.
    /// Creates and caches a FixedWindow limiter for the key on first use.
    /// IMPORTANT: Subsequent calls with different limiter parameters do not reconfigure the limiter.
    /// Use <see cref="ConfigureFixedWindow"/> (or Remove/Clear) to change limits after the first call.
    /// </summary>
    /// <typeparam name="T">Return type of the action.</typeparam>
    /// <param name="key">Logical limiter key (e.g., "BlandAI.SendCall"). Multiple callers sharing the same key share the same limiter.</param>
    /// <param name="maxPermitsPerWindow">Maximum number of operations allowed per window.</param>
    /// <param name="window">Time window for the rate limit.</param>
    /// <param name="queueLimit">Maximum number of pending requests allowed to wait for a permit.</param>
    /// <param name="action">The async operation to execute once a permit is acquired.</param>
    /// <param name="cancellationToken">Cancellation token for waiting on a permit and for the action itself.</param>
    public static async Task<T> ExecuteAsync<T>(
        string key,
        int maxPermitsPerWindow,
        TimeSpan window,
        int queueLimit,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var limiter = _limiters.GetOrAdd(key, _ =>
            new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = maxPermitsPerWindow,
                Window = window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = queueLimit,
                AutoReplenishment = true
            }));

        // Wait until a permit can be acquired; if the queue is full, this returns a failed lease immediately.
        using var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            // When queue is full, the lease is not acquired; caller can handle this as a throttling failure.
            // We surface a consistent exception type so the caller can either retry or record the failure.
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan ra) ? ra : (TimeSpan?)null;
            throw new ThrottleRejectedException(key, retryAfter);
        }

        // Execute the operation under the acquired permit.
        return await action(cancellationToken).ConfigureAwait(false);
    }

    // -----------------------------
    // Configuration & Factory
    // -----------------------------

    /// <summary>
    /// Configure a default factory to create limiters on-demand for unknown keys.
    /// If not set, calling ExecuteAsync with an unknown key throws InvalidOperationException.
    /// </summary>
    /// <param name="factory">Factory that returns a new <see cref="RateLimiter"/> for a key.</param>
    public static void ConfigureDefaultFactory(Func<string, RateLimiter> factory) => _defaultFactory = factory;

    /// <summary>
    /// Removes the default factory so unknown keys must be explicitly configured.
    /// </summary>
    public static void ClearDefaultFactory() => _defaultFactory = null;

    /// <summary>
    /// Returns true if a limiter is configured for the given key.
    /// </summary>
    public static bool IsConfigured(string key) => _limiters.ContainsKey(key);

    /// <summary>
    /// Removes and disposes the limiter for a key, if present.
    /// </summary>
    /// <returns>True if a limiter was removed; otherwise false.</returns>
    public static bool Remove(string key)
    {
        if (_limiters.TryRemove(key, out var old))
        {
            old.Dispose();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes and disposes all configured limiters.
    /// </summary>
    public static void Clear()
    {
        foreach (var kvp in _limiters)
        {
            if (_limiters.TryRemove(kvp.Key, out var limiter))
            {
                limiter.Dispose();
            }
        }
    }

    // -----------------------------
    // Ensure/Configure helpers
    // -----------------------------

    /// <summary>
    /// Create or replace a FixedWindow limiter for the given key.
    /// </summary>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="permitLimit">Max operations permitted per window.</param>
    /// <param name="window">Window duration.</param>
    /// <param name="queueLimit">Max queued operations awaiting permits.</param>
    /// <param name="queueOrder">Order in which queued requests acquire permits.</param>
    /// <param name="autoReplenishment">If true, permits replenish automatically on a timer; otherwise manual.</param>
    public static void ConfigureFixedWindow(
        string key,
        int permitLimit,
        TimeSpan window,
        int queueLimit = 0,
        QueueProcessingOrder queueOrder = QueueProcessingOrder.OldestFirst,
        bool autoReplenishment = true)
        => Configure(key, new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = queueLimit,
            QueueProcessingOrder = queueOrder,
            AutoReplenishment = autoReplenishment
        }));

    /// <summary>
    /// Create or replace a SlidingWindow limiter for the given key.
    /// Sliding windows help smooth bursts by splitting the window into segments.
    /// </summary>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="permitLimit">Max operations permitted per window.</param>
    /// <param name="window">Total sliding window duration.</param>
    /// <param name="segmentsPerWindow">Number of segments to split the window (higher smooths bursts more).</param>
    /// <param name="queueLimit">Max queued operations awaiting permits.</param>
    /// <param name="queueOrder">Order in which queued requests acquire permits.</param>
    /// <param name="autoReplenishment">If true, replenishes automatically; otherwise manual.</param>
    public static void ConfigureSlidingWindow(
        string key,
        int permitLimit,
        TimeSpan window,
        int segmentsPerWindow,
        int queueLimit = 0,
        QueueProcessingOrder queueOrder = QueueProcessingOrder.OldestFirst,
        bool autoReplenishment = true)
        => Configure(key, new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            SegmentsPerWindow = segmentsPerWindow,
            QueueLimit = queueLimit,
            QueueProcessingOrder = queueOrder,
            AutoReplenishment = autoReplenishment
        }));

    /// <summary>
    /// Create or replace a TokenBucket limiter for the given key.
    /// Token bucket commonly models "X tokens per period" APIs. Bursts up to <paramref name="tokenLimit"/> are allowed
    /// if tokens are available, replenished by <paramref name="tokensPerPeriod"/> every <paramref name="replenishmentPeriod"/>.
    /// </summary>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="tokenLimit">Max tokens the bucket can hold (burst size).</param>
    /// <param name="tokensPerPeriod">Tokens added per replenishment period.</param>
    /// <param name="replenishmentPeriod">How often to add tokens.</param>
    /// <param name="autoReplenishment">If true, tokens replenish automatically; otherwise manual.</param>
    /// <param name="queueLimit">Max queued operations awaiting tokens.</param>
    /// <param name="queueOrder">Order in which queued requests acquire tokens.</param>
    public static void ConfigureTokenBucket(
        string key,
        int tokenLimit,
        int tokensPerPeriod,
        TimeSpan replenishmentPeriod,
        bool autoReplenishment = true,
        int queueLimit = 0,
        QueueProcessingOrder queueOrder = QueueProcessingOrder.OldestFirst)
        => Configure(key, new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = tokenLimit,
            TokensPerPeriod = tokensPerPeriod,
            ReplenishmentPeriod = replenishmentPeriod,
            AutoReplenishment = autoReplenishment,
            QueueLimit = queueLimit,
            QueueProcessingOrder = queueOrder
        }));

    /// <summary>
    /// Create or replace a Concurrency limiter (limits number of concurrent in-flight executions) for the given key.
    /// This does not limit the rate over time, only simultaneous concurrency.
    /// </summary>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="permitLimit">Max concurrent operations allowed.</param>
    /// <param name="queueLimit">Max queued operations awaiting an available slot.</param>
    /// <param name="queueOrder">Order in which queued requests start execution.</param>
    public static void ConfigureConcurrency(
        string key,
        int permitLimit,
        int queueLimit = 0,
        QueueProcessingOrder queueOrder = QueueProcessingOrder.OldestFirst)
        => Configure(key, new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = permitLimit,
            QueueLimit = queueLimit,
            QueueProcessingOrder = queueOrder
        }));

    private static void Configure(string key, RateLimiter newLimiter)
    {
        // Swap the limiter atomically; dispose the old one.
        var existing = _limiters.AddOrUpdate(
            key,
            addValueFactory: _ => newLimiter,
            updateValueFactory: (_, old) =>
            {
                old.Dispose();
                return newLimiter;
            });

        // Defensive cleanup (should rarely/never happen). If for some reason the new limiter is not stored,
        // make sure we don't leak the instance we created.
        if (!ReferenceEquals(existing, newLimiter) && !_limiters.ContainsKey(key))
        {
            newLimiter.Dispose();
        }
    }

    private static RateLimiter GetOrCreateFromDefault(string key)
    {
        if (_limiters.TryGetValue(key, out var limiter))
        {
            return limiter;
        }

        if (_defaultFactory is null)
            throw new InvalidOperationException($"No limiter configured for key '{key}', and no default factory set. Call ConfigureFixedWindow/SlidingWindow/TokenBucket/Concurrency first, or set a default factory.");

        var created = _defaultFactory(key);
        // Try to add; if a concurrent thread added one already, dispose ours and return the existing.
        if (_limiters.TryAdd(key, created))
            return created;

        created.Dispose();
        return _limiters[key];
    }

    // ---------------------------------
    // Execute using preconfigured limiter
    // ---------------------------------

    /// <summary>
    /// Executes an async action using a pre-configured limiter for <paramref name="key"/> (or one created by the default factory).
    /// If <paramref name="waitTimeout"/> is provided, waiting for a permit is capped by this timeout; otherwise it waits until acquired or canceled.
    /// </summary>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="action">Action to execute once a permit is acquired.</param>
    /// <param name="waitTimeout">Optional timeout for waiting to acquire a permit (does not cancel the action).</param>
    /// <param name="cancellationToken">Cancellation for acquisition and the action.</param>
    public static Task ExecuteAsync(
        string key,
        Func<CancellationToken, Task> action,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default)
        => ExecuteInternalAsync<object?>(
            key,
            async ct => { await action(ct).ConfigureAwait(false); return null; },
            waitTimeout,
            cancellationToken);

    /// <summary>
    /// Executes an async action using a pre-configured limiter for <paramref name="key"/> (or one created by the default factory).
    /// If <paramref name="waitTimeout"/> is provided, waiting for a permit is capped by this timeout; otherwise it waits until acquired or canceled.
    /// </summary>
    /// <typeparam name="T">Return type of the action.</typeparam>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="action">Action to execute once a permit is acquired.</param>
    /// <param name="waitTimeout">Optional timeout for waiting to acquire a permit (does not cancel the action).</param>
    /// <param name="cancellationToken">Cancellation for acquisition and the action.</param>
    public static Task<T> ExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default)
        => ExecuteInternalAsync<T>(key, action, waitTimeout, cancellationToken);

    /// <summary>
    /// Attempts to execute an async action using a pre-configured limiter.
    /// Returns <c>false</c> if the queue is full or acquisition times out before a permit can be obtained.
    /// </summary>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="action">Action to execute once a permit is acquired.</param>
    /// <param name="waitTimeout">Optional timeout for waiting to acquire a permit.</param>
    /// <param name="cancellationToken">Cancellation for acquisition and the action.</param>
    public static async Task<bool> TryExecuteAsync(
        string key,
        Func<CancellationToken, Task> action,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var (success, _, _) = await TryExecuteInternalAsync<object?>(
            key,
            async ct => { await action(ct).ConfigureAwait(false); return null; },
            waitTimeout,
            cancellationToken).ConfigureAwait(false);

        return success;
    }

    /// <summary>
    /// Attempts to execute an async action using a pre-configured limiter.
    /// Returns a tuple indicating success and the action's result (if any). On acquisition failure, result is default.
    /// </summary>
    /// <typeparam name="T">Return type of the action.</typeparam>
    /// <param name="key">Limiter key (case-insensitive).</param>
    /// <param name="action">Action to execute once a permit is acquired.</param>
    /// <param name="waitTimeout">Optional timeout for waiting to acquire a permit.</param>
    /// <param name="cancellationToken">Cancellation for acquisition and the action.</param>
    public static async Task<(bool success, T? result)> TryExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var (success, result, _) = await TryExecuteInternalAsync(key, action, waitTimeout, cancellationToken).ConfigureAwait(false);
        return (success, result);
    }

    private static async Task<T> ExecuteInternalAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? waitTimeout,
        CancellationToken cancellationToken)
    {
        var limiter = GetOrCreateFromDefault(key);

        using var cts = waitTimeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        cts?.CancelAfter(waitTimeout!.Value);

        using var lease = await limiter.AcquireAsync(1, cts?.Token ?? cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan ra) ? ra : (TimeSpan?)null;
            // Timeouts or queue-full both manifest as non-acquired lease.
            throw new ThrottleRejectedException(key, retryAfter);
        }

        return await action(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(bool success, T? result, TimeSpan? retryAfter)> TryExecuteInternalAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? waitTimeout,
        CancellationToken cancellationToken)
    {
        var limiter = GetOrCreateFromDefault(key);

        using var cts = waitTimeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;

        cts?.CancelAfter(waitTimeout!.Value);

        using var lease = await limiter.AcquireAsync(1, cts?.Token ?? cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan ra) ? ra : (TimeSpan?)null;
            return (false, default, retryAfter);
        }

        var result = await action(cancellationToken).ConfigureAwait(false);
        return (true, result, null);
    }
}

/// <summary>
/// Indicates a request could not be queued or waited for due to throttling (queue full or acquisition timeout).
/// </summary>
public sealed class ThrottleRejectedException(string key, TimeSpan? retryAfter) : Exception(retryAfter.HasValue
            ? $"Operation throttled for '{key}'. Retry after {retryAfter.Value.TotalMilliseconds:n0} ms."
            : $"Operation throttled for '{key}'. Queue limit exceeded or acquisition timed out.")
{
    /// <summary>
    /// Logical limiter key that rejected the request.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Suggested "retry-after" duration when available from the limiter metadata; may be null.
    /// </summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}