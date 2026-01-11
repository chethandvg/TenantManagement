# Background Jobs - TentMan

## Overview

TentMan uses **Hangfire** as the background job processing framework to ensure reliable and automated execution of billing-related tasks. Hangfire provides persistent job storage, automatic retries, and a built-in dashboard for monitoring.

## Architecture

### Framework Choice: Hangfire

**Why Hangfire?**
- **Persistent Storage**: Jobs are stored in SQL Server, ensuring reliability even after application restarts
- **Automatic Retries**: Built-in retry logic with exponential backoff for transient failures
- **Dashboard**: Web-based UI for monitoring jobs, viewing history, and manual execution
- **.NET Integration**: Native .NET library with excellent ASP.NET Core integration
- **No External Dependencies**: Runs in-process with the API, no separate service required
- **Production-Ready**: Battle-tested in production environments

**Comparison with Alternatives:**
- **Quartz.NET**: More complex configuration, less modern dashboard
- **Azure Functions**: Requires Azure infrastructure, additional cost
- **AWS Lambda**: Requires AWS infrastructure, vendor lock-in

### Components

```
┌─────────────────────────────────────────────┐
│           TentMan.Api (ASP.NET Core)        │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │    Hangfire Dashboard                │  │
│  │    /hangfire                         │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │    Hangfire Server (Background)      │  │
│  │    - Processes jobs                  │  │
│  │    - 5 concurrent workers            │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │    Recurring Job Scheduler           │  │
│  │    - Monthly Rent (26th @ 2 AM)      │  │
│  │    - Utility Billing (Mon @ 3 AM)    │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────┐
│        SQL Server (Hangfire Schema)         │
│                                             │
│  - Job Queue                                │
│  - Job State History                        │
│  - Recurring Jobs                           │
│  - Job Parameters                           │
│  - Server List                              │
└─────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────┐
│   Application.BackgroundJobs (Job Logic)    │
│                                             │
│  - MonthlyRentGenerationJob                 │
│  - UtilityBillingJob                        │
└─────────────────────────────────────────────┘
```

## Jobs

### 1. Monthly Rent Generation Job

**Purpose**: Automatically generates monthly rent invoices for all active leases across all organizations.

**Schedule**: 
- **Cron**: `0 2 26 * *` (At 02:00 on day 26 of every month)
- **Rationale**: Runs 5 days before the month starts to allow time for review and corrections

**Configuration**:
```csharp
RecurringJob.AddOrUpdate<MonthlyRentGenerationJob>(
    "monthly-rent-generation",
    job => job.ExecuteForNextMonthAsync(5),
    "0 2 26 * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

**Features**:
- Generates invoices for the **next month** (e.g., runs Jan 26 for February billing)
- **Multi-tenant**: Automatically iterates over all organizations in the database
- Validates job is running within expected time window (±1 day tolerance)
- Uses `ActualDaysInMonth` proration method by default
- Logs all successes and failures with detailed error messages per organization
- Supports partial failures (continues processing remaining organizations if one fails)
- Comprehensive error tracking with organization-specific error messages
- Throws exception on fatal failures for Hangfire retry mechanism

**Idempotency**: The underlying `InvoiceRunService` ensures idempotency by updating existing draft invoices rather than creating duplicates.

**Error Handling**:
- Logs individual lease failures but continues processing
- Throws exception for critical failures to trigger Hangfire retries
- Automatic retry with exponential backoff (Hangfire default: 10 attempts)

**Manual Execution**:
```csharp
// Via Hangfire Dashboard or code
BackgroundJob.Enqueue<MonthlyRentGenerationJob>(
    job => job.ExecuteAsync(orgId, billingStart, billingEnd, prorationMethod));
```

---

### 2. Utility Billing Job

**Purpose**: Generates utility invoices for leases with pending utility statements across all organizations.

**Schedule**: 
- **Cron**: `0 3 * * 1` (At 03:00 every Monday)
- **Rationale**: Weekly execution to bill newly finalized utility statements

**Configuration**:
```csharp
RecurringJob.AddOrUpdate<UtilityBillingJob>(
    "utility-billing",
    job => job.ExecuteForCurrentPeriodAsync(),
    "0 3 * * 1",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

**Features**:
- Processes utility statements for the **current month**
- **Multi-tenant**: Automatically iterates over all organizations in the database
- Only bills utility statements that are finalized but not yet invoiced
- Logs execution results and errors per organization
- Supports partial failures (continues processing remaining organizations)
- **Note**: Currently a placeholder - full implementation pending UtilityStatementRepository

**Idempotency**: Will be ensured by checking for existing utility invoice lines before generation.

**Error Handling**:
- Logs failures but does not throw (prevents endless retries for unimplemented feature)
- Will throw exceptions once fully implemented

**Manual Execution**:
```csharp
// Via Hangfire Dashboard or code
BackgroundJob.Enqueue<UtilityBillingJob>(
    job => job.ExecuteAsync(orgId, billingStart, billingEnd));
```

---

## Configuration

### Hangfire Settings

**DependencyInjection.cs (Infrastructure)**:
```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        SchemaName = "Hangfire"
    }));

services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid()}";
});
```

**Key Settings**:
- **Worker Count**: 5 concurrent workers for processing jobs
- **Schema**: `Hangfire` (separate from application tables)
- **Queue Poll Interval**: Zero (immediate polling for fast execution)
- **Sliding Invisibility**: 5 minutes (job appears "in progress" for 5 min)

### Dashboard Configuration

**Program.cs (API)**:
```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    DashboardTitle = "TentMan Background Jobs",
    StatsPollingInterval = 2000
});
```

**Access**:
- **Development**: Open to all users for testing (`/hangfire`)
- **Production**: Restricted to authenticated administrators

**Dashboard Features**:
- View all jobs (recurring, enqueued, processing, succeeded, failed)
- Manually trigger jobs
- View job history and execution logs
- Monitor server status and worker count
- Retry failed jobs

---

## Job Execution Flow

### Monthly Rent Generation

```
┌─────────────────────────────────────┐
│  Hangfire Scheduler                 │
│  (Triggers on 26th @ 2 AM UTC)      │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  MonthlyRentGenerationJob           │
│  - Calculates next month's period   │
│  - Logs start of execution          │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  InvoiceRunService                  │
│  - Creates InvoiceRun entity        │
│  - Fetches all active leases        │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  For Each Lease:                    │
│  - InvoiceGenerationService         │
│  - Generate rent line items         │
│  - Generate recurring charge items  │
│  - Calculate totals                 │
│  - Save invoice (draft)             │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  InvoiceRun Completion              │
│  - Update success/failure counts    │
│  - Set status (Completed/Failed)    │
│  - Log results                      │
└─────────────────────────────────────┘
```

### Error Handling

1. **Individual Lease Failure**: Logged, processing continues to next lease
2. **Complete Run Failure**: Exception thrown, Hangfire retries
3. **Transient Errors**: Automatic retry with exponential backoff
4. **Persistent Failures**: Logged in Hangfire dashboard, admin notified

---

## Monitoring & Alerting

### Dashboard Monitoring

**Access**: `https://your-api-url/hangfire`

**Metrics to Monitor**:
- **Succeeded Jobs**: Should increase after each scheduled run
- **Failed Jobs**: Should be zero or low; investigate if increasing
- **Processing Time**: Should be consistent; spikes indicate issues
- **Worker Utilization**: Should be balanced across workers

### Logging

All jobs log to the application's standard logging infrastructure:

```csharp
_logger.LogInformation(
    "Monthly rent generation completed. Total: {TotalLeases}, Success: {SuccessCount}, Failures: {FailureCount}",
    result.TotalLeases, result.SuccessCount, result.FailureCount);
```

**Log Levels**:
- **Information**: Job start, completion, success counts
- **Warning**: Partial failures, individual lease errors
- **Error**: Complete job failures, exceptions

### Alerting (Future Enhancement)

**Recommended Integrations**:
- Email notifications on job failures (via application logging or Hangfire extensions)
- Slack/Teams notifications for critical failures
- Application Insights or similar APM for advanced monitoring

---

## Retry Logic

### Hangfire Built-in Retries

**Default Behavior**:
- **Retry Count**: 10 attempts
- **Backoff**: Exponential (1 min, 2 min, 4 min, 8 min, ...)
- **Max Backoff**: Capped at reasonable limit

**Custom Retry Configuration** (if needed):
```csharp
[AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 900 })]
public async Task ExecuteAsync(...)
{
    // Job logic
}
```

### Idempotency Guarantees

**MonthlyRentGenerationJob**:
- `InvoiceGenerationService` checks for existing draft invoices
- Updates existing invoice instead of creating duplicate
- Safe to retry without data corruption

**UtilityBillingJob**:
- Will check for existing utility invoice lines (when implemented)
- Skips already-billed utility statements

---

## Database Schema

Hangfire creates the following tables in the `Hangfire` schema:

- `AggregatedCounter`: Performance counters
- `Counter`: Job counters
- `Hash`: Job parameters and metadata
- `Job`: Job definitions
- `JobParameter`: Job arguments
- `JobQueue`: Job queue
- `List`: Job lists
- `Schema`: Schema version
- `Server`: Active servers
- `Set`: Job sets (recurring, scheduled)
- `State`: Job state history

**Migration**: Hangfire automatically creates and migrates its schema on startup.

---

## Operational Tasks

### Add a New Recurring Job

1. **Create Job Class** in `Application/BackgroundJobs`:
```csharp
public class NewBillingJob
{
    public async Task ExecuteAsync(...)
    {
        // Job logic
    }
}
```

2. **Register in DI** (`Infrastructure/DependencyInjection.cs`):
```csharp
services.AddScoped<NewBillingJob>();
```

3. **Schedule Job** (`Api/Program.cs`):
```csharp
RecurringJob.AddOrUpdate<NewBillingJob>(
    "job-name",
    job => job.ExecuteAsync(...),
    "0 0 * * *", // Cron expression
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
```

### Remove a Recurring Job

```csharp
RecurringJob.RemoveIfExists("job-name");
```

### Trigger Job Manually

**Via Dashboard**:
1. Navigate to `/hangfire`
2. Go to "Recurring Jobs"
3. Click "Trigger now" for the desired job

**Via Code**:
```csharp
RecurringJob.Trigger("monthly-rent-generation");
```

### View Job History

1. Navigate to `/hangfire`
2. Click "Jobs" → "Succeeded" or "Failed"
3. Filter by job name or time range

---

## Production Deployment

### Pre-Deployment Checklist

- [ ] Database connection string configured
- [ ] Hangfire dashboard authentication enabled
- [ ] Worker count appropriate for server capacity
- [ ] Logging configured (Application Insights, Seq, etc.)
- [ ] Alerting configured for failed jobs
- [ ] Backup and recovery plan for Hangfire database

### Scaling Considerations

**Single Server**:
- Default configuration (5 workers)
- Suitable for small to medium workloads

**Multiple Servers**:
- Hangfire automatically distributes jobs across servers
- Each server has its own worker pool
- No additional configuration needed (uses SQL Server for coordination)

**High Availability**:
- Run Hangfire server on multiple API instances
- Jobs are not lost if one server goes down
- Automatic failover via SQL Server storage

### Performance Tuning

**For Large Organizations**:
- Increase worker count: `options.WorkerCount = 10`
- Batch job processing: Process leases in smaller chunks
- Optimize database queries: Add indexes if needed

**For Low Latency**:
- Reduce `QueuePollInterval` (already at zero)
- Increase worker count for parallel execution

---

## Troubleshooting

### Job Fails Repeatedly

**Check**:
1. Dashboard → Failed Jobs → View exception
2. Application logs for detailed errors
3. Database connectivity
4. InvoiceRunService dependencies

**Common Issues**:
- Database timeout (increase `CommandTimeout`)
- Memory issues (reduce batch size)
- External service unavailable (add retry logic)

### Jobs Not Running

**Check**:
1. Dashboard → Servers (should show active server)
2. Recurring Jobs (should be enabled)
3. Database connection
4. Application logs for startup errors

**Fix**:
- Restart API application
- Verify database schema created
- Check firewall/network connectivity

### Dashboard Not Accessible

**Check**:
1. URL: `/hangfire` (must be exact)
2. Authentication (in production)
3. CORS settings (if accessing from different origin)

---

## Future Enhancements

- [ ] Implement notification service for job failures (email/Slack)
- [ ] Add dashboard analytics (job duration trends, failure rates)
- [ ] Implement job prioritization (critical vs. routine jobs)
- [ ] Add support for organization-specific job schedules
- [ ] Create admin UI for managing job schedules
- [ ] Add job execution metrics to application monitoring

---

## References

- **Hangfire Documentation**: https://docs.hangfire.io
- **Cron Expression Guide**: https://crontab.guru
- **Invoice Run Service**: `src/TentMan.Application/Billing/Services/InvoiceRunService.cs`
- **Background Jobs**: `src/TentMan.Application/BackgroundJobs/`

---

**Last Updated**: 2026-01-11  
**Maintainer**: TentMan Development Team
