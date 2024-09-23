using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Package.Infrastructure.Messaging.ServiceBus;
public abstract class ServiceBusProcessorBase : IServiceBusReceiver
{
    private readonly ILogger<ServiceBusProcessorBase> _logger;
    private readonly ServiceBusProcessorSettingsBase _settings;
    private readonly ServiceBusClient _sbClient;
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _sbProcessors = new();
    private static readonly Lock _lock = new();

    protected ServiceBusProcessorBase(ILogger<ServiceBusProcessorBase> logger, IOptions<ServiceBusProcessorSettingsBase> settings,
        IAzureClientFactory<ServiceBusClient> clientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _sbClient = clientFactory.CreateClient(_settings.ServiceBusClientName);
    }

    /// <summary>
    /// Register ServiceBusProcessor on a queue or topic-subscription  
    /// </summary>
    /// <param name="queueOrTopicName"></param>
    /// <param name="subscriptionName">Applies only to Topic, ignored for queue</param>
    /// <param name="funcProcess"></param>
    /// <param name="funcError"></param>
    public void RegisterProcessor(string queueOrTopicName, string? subscriptionName, Func<ProcessMessageEventArgs, Task> funcProcess, Func<ProcessErrorEventArgs, Task> funcError)
    {
        _logger.LogInformation("RegisterProcessor Start - {QueueOrTopicName}-{SubscriptionName}}", queueOrTopicName, subscriptionName);
        string pre = subscriptionName != null ? "s" : "q";
        string key = $"{pre}.{queueOrTopicName}{(pre == "s" ? $".{subscriptionName}" : "")}";
        ServiceBusProcessor sbProcessor = GetServiceBusProcessor(key, subscriptionName);
        sbProcessor.ProcessMessageAsync += funcProcess;
        sbProcessor.ProcessErrorAsync += funcError;
        _logger.LogInformation("RegisterProcessor Finish - {QueueOrTopicName}-{SubscriptionName}}", queueOrTopicName, subscriptionName);
    }

    public async Task StartProcessingAsync(string queueOrTopicName, string? subscriptionName = null, CancellationToken cancellationToken = default)
    {
        string pre = subscriptionName != null ? "s" : "q";
        string key = $"{pre}.{queueOrTopicName}{(pre == "s" ? $".{subscriptionName}" : "")}";
        if (!_sbProcessors.TryGetValue(key, out ServiceBusProcessor? sbProcessor)) throw new InvalidOperationException($"ServiceBusProcessor '{key}' does not exist in the collection so cannot be started.");
        await sbProcessor.StartProcessingAsync(cancellationToken); //background thread
    }

    public async Task StopProcessingAsync(string queueOrTopicName, string? subscriptionName = null, CancellationToken cancellationToken = default)
    {
        string pre = subscriptionName != null ? "s" : "q";
        string key = $"{pre}.{queueOrTopicName}{(pre == "s" ? $".{subscriptionName}" : "")}";
        if (!_sbProcessors.TryGetValue(key, out ServiceBusProcessor? sbProcessor)) throw new InvalidOperationException($"ServiceBusProcessor '{key}' does not exist in the collection so cannot be stopped.");
        await sbProcessor.StopProcessingAsync(cancellationToken);
    }

    private ServiceBusProcessor GetServiceBusProcessor(string queueOrTopicName, string? subscriptionName = null)
    {
        bool sub = subscriptionName != null;
        string key = $"{(sub ? "s" : "q")}.{queueOrTopicName}{(sub ? $".{subscriptionName}" : "")}";

        if (_sbProcessors.TryGetValue(key, out var result)) return result;

        lock (_lock)
        {
            // Try to fetch from cache again now that we have entered the critical section
            if (_sbProcessors.TryGetValue(key, out result)) return result;

            // Fetch data from source (async), update cache and return.
            ServiceBusProcessor sbp = sub
                ? _sbClient.CreateProcessor(queueOrTopicName, subscriptionName, _settings.ServiceBusProcessorOptions)
                : _sbClient.CreateProcessor(queueOrTopicName, _settings.ServiceBusProcessorOptions);

            _sbProcessors.TryAdd(key, sbp);

            return sbp;
        }
    }
}
