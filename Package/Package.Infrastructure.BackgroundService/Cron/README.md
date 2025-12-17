# Scalable Cron Job Background Service

This refactored cron job system eliminates the need for decision logic in the background service by using a handler pattern with dependency injection.

## Key Benefits

1. **No Decision Logic**: Each job type has its own dedicated handler
2. **Scalable**: Add new job types without modifying existing code  
3. **Testable**: Handlers can be easily unit tested in isolation
4. **Type Safe**: Each job type is strongly typed with its configuration
5. **Clean Separation**: Business logic is separated from scheduling logic

## Architecture

### Components

1. **ICronJobHandler<T>**: Interface for job-specific logic
2. **CronBackgroundService<T>**: Generic background service that uses handlers
3. **Job Settings Classes**: Configuration classes inheriting from CronJobSettings
4. **Handler Classes**: Implement ICronJobHandler for specific job types

### How it Works

1. Each job type (e.g., `CustomCronJob`, `EmailCronJob`) extends `CronJobSettings`
2. Each job type has a corresponding handler implementing `ICronJobHandler<T>`
3. The generic `CronBackgroundService<T>` resolves the handler via DI and delegates execution
4. Multiple job instances can be configured for the same job type with different schedules

## Usage

### 1. Define a Job Settings Class

```csharp
public class CustomCronJob : CronJobSettings
{
    public string? SomeUrl { get; set; }
    public string? SomeTopicOrQueue { get; set; }
}
```

### 2. Create a Handler

```csharp
public class CustomCronJobHandler : ICronJobHandler<CustomCronJob>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CustomCronJobHandler> _logger;

    public CustomCronJobHandler(IServiceScopeFactory serviceScopeFactory, ILogger<CustomCronJobHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(string traceId, CustomCronJob cronJob, CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("{CronJob} - Start scheduled background work {Runtime}", cronJob.JobName, DateTime.Now);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var todoService = scope.ServiceProvider.GetRequiredService<ITodoService>();
            
            // Your job logic here
            await todoService.DoSomething();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{CronJob} - Failed during scheduled background work.", cronJob.JobName);
            throw; // Re-throw to let the base service handle it
        }

        _logger.LogInformation("{CronJob} - Complete scheduled background work {Runtime}", cronJob.JobName, DateTime.Now);
    }
}
```

### 3. Register Services

```csharp
// In Program.cs or Startup.cs

// Configure the cron job settings
services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(
    configuration.GetSection("CronServiceSettings"));

// Register the handler and background service
services.AddCronJob<CustomCronJob, CustomCronJobHandler>();

// Or use the extension method for multiple jobs
services.AddCronJobHandlers();
```

### 4. Configure Settings (appsettings.json)

```json
{
  "CronServiceSettings": {
    "CronJobs": [
      {
        "JobName": "CustomJob1",
        "Cron": "0 */5 * * * *",
        "SleepIntervalSeconds": 30,
        "LockSingleInstance": true,
        "SomeUrl": "https://api.example.com",
        "SomeTopicOrQueue": "my-topic"
      }
    ]
  }
}
```

Multiple specialized handlers
```csharp
public class Job1Handler : ICronJobHandler<Job1Settings> { /* Job 1 logic */ }
public class Job2Handler : ICronJobHandler<Job2Settings> { /* Job 2 logic */ }

// Registration
services.AddCronJob<Job1Settings, Job1Handler>();
services.AddCronJob<Job2Settings, Job2Handler>();
```

## Adding New Job Types

1. **Create a settings class** inheriting from `CronJobSettings`
2. **Create a handler** implementing `ICronJobHandler<T>`  
3. **Register the pair** using `AddCronJob<TSettings, THandler>()`
4. **Configure the job** in appsettings.json

No modifications to existing code required!

## Example: Multiple Job Types

```csharp
// Email processing job
public class EmailCronJob : CronJobSettings
{
    public string? SmtpServer { get; set; }
    public string? EmailTemplate { get; set; }
    public int BatchSize { get; set; } = 100;
}

public class EmailCronJobHandler : ICronJobHandler<EmailCronJob>
{
    public async Task ExecuteAsync(string traceId, EmailCronJob cronJob, CancellationToken stoppingToken = default)
    {
        // Email processing logic
    }
}

// Data cleanup job  
public class CleanupCronJob : CronJobSettings
{
    public int RetentionDays { get; set; } = 30;
    public string? TableNames { get; set; }
}

public class CleanupCronJobHandler : ICronJobHandler<CleanupCronJob>
{
    public async Task ExecuteAsync(string traceId, CleanupCronJob cronJob, CancellationToken stoppingToken = default)
    {
        // Cleanup logic
    }
}

// Registration
services.AddCronJob<EmailCronJob, EmailCronJobHandler>();
services.AddCronJob<CleanupCronJob, CleanupCronJobHandler>();
```

## Configuration for Multiple Job Types

```json
{
  "EmailCronJobs": {
    "CronJobs": [
      {
        "JobName": "DailyEmailReport",
        "Cron": "0 0 8 * * MON-FRI",
        "SmtpServer": "smtp.company.com",
        "EmailTemplate": "daily-report",
        "BatchSize": 50
      }
    ]
  },
  "CleanupCronJobs": {
    "CronJobs": [
      {
        "JobName": "WeeklyCleanup", 
        "Cron": "0 0 2 * * SUN",
        "RetentionDays": 90,
        "TableNames": "Logs,TempData"
      }
    ]
  }
}