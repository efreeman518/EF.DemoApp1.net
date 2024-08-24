using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionTimerTrigger(ILogger<FunctionTimerTrigger> logger, IConfiguration configuration, IOptions<Settings1> settings)
{
    [Function(nameof(FunctionTimerTrigger))]
    [ExponentialBackoffRetry(5, "00:00:05", "00:15:00")]
    public async Task Run([TimerTrigger("%TimerCron%")] TimerInfo timerInfo)
    {
        _ = configuration.GetHashCode();
        _ = settings.GetHashCode();

        logger.Log(LogLevel.Information, "TimerTrigger - Start {ExecutionUtc}", DateTime.UtcNow);

        //await some service call
        await Task.CompletedTask;

        logger.Log(LogLevel.Information, "TimerTrigger - Finish {ExecutionUtc} {NextSchedule}", DateTime.UtcNow, timerInfo.ScheduleStatus?.Next);
    }
}
