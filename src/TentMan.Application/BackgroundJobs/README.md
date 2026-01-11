# Background Jobs

This folder contains background job definitions that are executed by Hangfire on a recurring schedule.

## Jobs

### MonthlyRentGenerationJob
Automatically generates monthly rent invoices for all active leases across all organizations.

**Schedule**: 26th of each month at 2:00 AM UTC  
**Purpose**: Generate invoices 5 days before the billing period starts  
**Multi-tenant**: Iterates over all organizations automatically  
**Idempotency**: Safe to retry; updates existing draft invoices  

### UtilityBillingJob
Generates utility invoices for leases with finalized utility statements across all organizations.

**Schedule**: Every Monday at 3:00 AM UTC  
**Purpose**: Process newly finalized utility statements  
**Multi-tenant**: Iterates over all organizations automatically  
**Status**: Placeholder implementation (full implementation pending)

## Architecture

Jobs are:
1. **Defined** in this folder (Application layer)
2. **Registered** in `Infrastructure/DependencyInjection.cs`
3. **Scheduled** in `Api/Program.cs` using cron expressions
4. **Executed** by Hangfire server (runs in-process with API)
5. **Monitored** via Hangfire dashboard at `/hangfire`

## Adding a New Job

1. Create job class in this folder:
```csharp
public class NewBillingJob
{
    private readonly IYourService _service;
    private readonly ILogger<NewBillingJob> _logger;

    public NewBillingJob(IYourService service, ILogger<NewBillingJob> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task ExecuteAsync(...)
    {
        _logger.LogInformation("Starting job...");
        // Job logic
    }
}
```

2. Register in `Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<NewBillingJob>();
```

3. Schedule in `Api/Program.cs`:
```csharp
RecurringJob.AddOrUpdate<NewBillingJob>(
    "job-name",
    job => job.ExecuteAsync(...),
    "0 0 * * *", // Daily at midnight
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

## Best Practices

- ✅ Use `ILogger` for comprehensive logging
- ✅ Make jobs idempotent (safe to retry)
- ✅ Handle partial failures gracefully
- ✅ Use descriptive job names
- ✅ Document the schedule and purpose
- ✅ Log successes and failures with details
- ✅ Use UTC timezone for consistency

## Documentation

See [Background Jobs Documentation](../../../docs/BACKGROUND_JOBS.md) for complete details on:
- Job execution flow
- Monitoring and alerting
- Error handling and retries
- Dashboard usage
- Troubleshooting

---

**Last Updated**: 2026-01-11  
**Maintainer**: TentMan Development Team
