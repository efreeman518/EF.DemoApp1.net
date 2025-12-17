Example: DI registration and appsettings for multiple cron job types

Program.cs (or wherever you configure services):

```csharp
using Package.Infrastructure.BackgroundServices;
using SampleApp.BackgroundServices.Scheduler;

var builder = WebApplication.CreateBuilder(args);

// Configure settings for each job type from configuration sections
builder.Services.Configure<CronJobBackgroundServiceSettings<CustomCronJob>>(builder.Configuration.GetSection("CustomCronJobs"));
builder.Services.Configure<CronJobBackgroundServiceSettings<EmailCronJob>>(builder.Configuration.GetSection("EmailCronJobs"));

// Register handlers and the generic cron hosted services
builder.Services.AddCronJob<CustomCronJob, CustomCronJobHandler>();
builder.Services.AddCronJob<EmailCronJob, EmailCronJobHandler>();

var app = builder.Build();
app.Run();
```

appsettings.json

```json
{
  "CustomCronJobs": {
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
  },
  "EmailCronJobs": {
    "CronJobs": [
      {
        "JobName": "EmailJob1",
        "Cron": "0 0 8 * * MON-FRI",
        "SleepIntervalSeconds": 600,
        "LockSingleInstance": true,
        "SmtpServer": "smtp.company.com",
        "EmailTemplate": "weekly-report",
        "BatchSize": 50
      }
    ]
  }
}
```

Notes:
- The `CronBackgroundService<T>` subscribes to configuration changes via `IOptionsMonitor<T>` and will cancel/restart running job tasks when settings change.
- Each job type has its own settings section and handler; add more by creating a settings class and handler and registering with `AddCronJob<,>()`.