using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionTimerTrigger(IConfiguration configuration, ILoggerFactory loggerFactory,
    IOptions<Settings1> settings)
{
    private readonly ILogger<FunctionTimerTrigger> _logger = loggerFactory.CreateLogger<FunctionTimerTrigger>();

    [Function("TimerTrigger")]
    [ExponentialBackoffRetry(5, "00:00:05", "00:15:00")]
    public async Task Run([TimerTrigger("%TimerCron%")] TimerInfo timerInfo)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();
        _logger.Log(LogLevel.Information, "TimerTrigger - Start {ExecutionUtc}", DateTime.UtcNow);

        //await some service call
        await Task.CompletedTask;

        _logger.Log(LogLevel.Information, "TimerTrigger - Finish {ExecutionUtc} {NextSchedule}", DateTime.UtcNow, timerInfo.ScheduleStatus?.Next);
    }
}
