using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions;

public class FunctionTimerTrigger
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionTimerTrigger> _logger;
    private readonly Settings1 _settings;

    public FunctionTimerTrigger(IConfiguration configuration, ILoggerFactory loggerFactory,
        IOptions<Settings1> settings)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FunctionTimerTrigger>();
        _settings = settings.Value;
    }

    //hangs local VS2022?
    [Function("TimerTrigger")]
    public async Task Run([TimerTrigger("%TimerCron%")] TimerInfo timerInfo)
    {
        _ = _configuration.GetHashCode();
        _ = _settings.GetHashCode();
        _logger.Log(LogLevel.Information, "TimerTrigger - Start {ExecutionUtc}", DateTime.UtcNow);

        //await some service call
        await Task.CompletedTask;

        _logger.Log(LogLevel.Information, "TimerTrigger - Finish {ExecutionUtc} {NextSchedule}", DateTime.UtcNow, timerInfo.ScheduleStatus?.Next);
    }
}
