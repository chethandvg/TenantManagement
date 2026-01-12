# Billing Engine Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the TentMan Billing Engine to production, including database migrations, configuration, monitoring, security considerations, and operational procedures.

---

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Database Migration Strategy](#database-migration-strategy)
3. [Configuration](#configuration)
4. [Deployment Steps](#deployment-steps)
5. [Monitoring & Logging](#monitoring--logging)
6. [Security Considerations](#security-considerations)
7. [Rollback Procedures](#rollback-procedures)
8. [Post-Deployment Verification](#post-deployment-verification)
9. [Troubleshooting](#troubleshooting)

---

## Pre-Deployment Checklist

### Database Preparation

- [ ] **Backup Production Database**
  ```bash
  # Create a full backup before migration
  sqlcmd -S <server> -d TentManDb -Q "BACKUP DATABASE TentManDb TO DISK = 'C:\Backups\TentManDb_PreBilling_$(date +%Y%m%d_%H%M%S).bak' WITH COMPRESSION;"
  ```

- [ ] **Test Migrations on Staging**
  - Apply all billing migrations to staging environment
  - Verify data integrity
  - Test rollback procedures
  - Measure migration execution time

- [ ] **Review Migration Scripts**
  - Review all billing-related migrations:
    - `20260110095843_AddBillingEngineTables.cs`
    - `20260110095910_SeedChargeTypeSystemRecords.cs`
    - `20260111220043_AddBillingEdgeCaseFields.cs`
    - `20260112000928_AddDefaultLeaseBillingSettings.cs`

### Configuration Preparation

- [ ] **Secure Secrets in Azure Key Vault**
  - Database connection strings
  - JWT secret keys
  - Any API keys or external service credentials

- [ ] **Update Application Settings**
  - `appsettings.Production.json` configured with production values
  - Hangfire worker count adjusted for production load
  - Billing job schedules configured
  - Logging levels set appropriately

### Infrastructure Preparation

- [ ] **Server Resources**
  - Adequate CPU and memory for Hangfire workers (recommended: 10 workers for production)
  - Database server has sufficient capacity
  - Disk space for Hangfire job storage

- [ ] **Monitoring Setup**
  - Application Insights configured (if using Azure)
  - Log aggregation service connected
  - Alert rules defined

---

## Database Migration Strategy

### Zero-Downtime Migration Approach

The billing engine migrations are designed to be backward-compatible and can be applied with minimal downtime:

#### Step 1: Apply Schema Changes
```bash
cd src/TentMan.Infrastructure
dotnet ef database update --startup-project ../TentMan.Api --connection "<connection-string>"
```

**Migration Sequence:**
1. `AddBillingEngineTables` - Creates all billing tables
2. `SeedChargeTypeSystemRecords` - Seeds system charge types
3. `AddBillingEdgeCaseFields` - Adds proration method and void reason fields
4. `AddDefaultLeaseBillingSettings` - Populates billing settings for existing leases

**Estimated Time:** 5-10 minutes (depending on database size)

#### Step 2: Data Verification Script

Run this script after migration to verify data integrity:

```sql
-- Verify all billing tables exist
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN (
    'ChargeTypes', 'LeaseBillingSettings', 'LeaseRecurringCharges',
    'Invoices', 'InvoiceLines', 'InvoiceRuns', 'InvoiceRunItems',
    'CreditNotes', 'CreditNoteLines', 'UtilityRatePlans', 
    'UtilityRateSlabs', 'UtilityStatements'
);

-- Verify system charge types seeded
SELECT Code, Name, IsSystemDefined 
FROM ChargeTypes 
WHERE IsSystemDefined = 1
ORDER BY Code;

-- Verify all active leases have billing settings
SELECT COUNT(*) as LeaseCount
FROM Leases l
WHERE l.IsDeleted = 0 
AND NOT EXISTS (
    SELECT 1 FROM LeaseBillingSettings lbs 
    WHERE lbs.LeaseId = l.Id
);
-- Expected result: 0

-- Verify Hangfire schema exists
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'Hangfire';
```

---

## Configuration

### Application Settings (Production)

**File:** `src/TentMan.Api/appsettings.Production.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "TentMan.Application.Billing": "Information",
      "TentMan.Application.BackgroundJobs": "Information",
      "Hangfire": "Warning"
    }
  },
  "Hangfire": {
    "WorkerCount": 10,
    "ServerName": "TentManAPI-Production"
  },
  "BillingSettings": {
    "DefaultBillingDay": 1,
    "DefaultPaymentTermDays": 0,
    "DefaultProrationMethod": "ActualDaysInMonth"
  },
  "BackgroundJobs": {
    "MonthlyRentGeneration": {
      "Schedule": "0 2 26 * *",
      "DaysBeforeMonthStart": 5
    },
    "UtilityBilling": {
      "Schedule": "0 3 * * 1"
    }
  }
}
```

### Environment Variables

Set these in your hosting environment (Azure App Service, IIS, etc.):

```bash
# Required
ConnectionStrings__Sql=<connection-string>
Jwt__Secret=<jwt-secret-key>

# Optional (if not using Azure Key Vault)
DatabaseSeeding__AdminPassword=<secure-admin-password>
```

### Hangfire Dashboard Access

The Hangfire dashboard is available at `/hangfire` and requires authentication.

**Authorization:** Only users with `SuperAdmin` or `Administrator` roles can access the dashboard.

**Configuration:** See `src/TentMan.Api/Authorization/HangfireDashboardAuthorizationFilter.cs`

---

## Deployment Steps

### Option 1: Azure App Service Deployment

1. **Build the Application**
   ```bash
   dotnet publish src/TentMan.Api/TentMan.Api.csproj -c Release -o ./publish
   ```

2. **Deploy via Azure CLI**
   ```bash
   az webapp deploy --resource-group <rg-name> --name <app-name> --src-path ./publish.zip --type zip
   ```

3. **Apply Database Migrations**
   ```bash
   # Option A: Run from deployment pipeline
   dotnet ef database update --startup-project src/TentMan.Api --connection "<connection-string>"
   
   # Option B: Enable automatic migrations on startup (Development only)
   # Set environment variable: ASPNETCORE_ENVIRONMENT=Development
   ```

4. **Verify Deployment**
   - Check application logs
   - Access `/health` endpoint
   - Access `/hangfire` dashboard

### Option 2: Docker Deployment

1. **Build Docker Image**
   ```bash
   docker build -t tentman-api:latest -f src/TentMan.Api/Dockerfile .
   ```

2. **Run Container**
   ```bash
   docker run -d \
     -p 8080:8080 \
     -e ConnectionStrings__Sql="<connection-string>" \
     -e Jwt__Secret="<jwt-secret>" \
     -e ASPNETCORE_ENVIRONMENT=Production \
     --name tentman-api \
     tentman-api:latest
   ```

3. **Apply Migrations**
   ```bash
   docker exec tentman-api dotnet ef database update
   ```

### Option 3: Traditional IIS Deployment

1. **Publish Application**
   ```bash
   dotnet publish -c Release -o C:\inetpub\wwwroot\tentman
   ```

2. **Configure IIS**
   - Create Application Pool (No Managed Code)
   - Set Application Pool identity
   - Configure environment variables in web.config

3. **Apply Migrations** (via Package Manager Console or command line)

---

## Monitoring & Logging

### Structured Logging

The billing engine uses structured logging with the following patterns:

```csharp
// Invoice generation
_logger.LogInformation("Starting invoice generation for lease {LeaseId} for period {PeriodStart} to {PeriodEnd}", 
    leaseId, periodStart, periodEnd);

// Background job execution
_logger.LogInformation("Monthly rent generation job started for {OrganizationCount} organizations", 
    organizations.Count);

// Error logging
_logger.LogError(ex, "Failed to generate invoice for lease {LeaseId}: {ErrorMessage}", 
    leaseId, ex.Message);
```

### Key Metrics to Monitor

#### Application Metrics
- **Invoice Generation Time**: Track time taken to generate invoices
- **Background Job Execution**: Monitor job success/failure rates
- **API Response Times**: Track billing API endpoint performance

#### Database Metrics
- **Query Performance**: Monitor slow queries on billing tables
- **Connection Pool**: Track connection pool usage
- **Deadlocks**: Monitor for deadlock errors

#### Business Metrics
- **Invoice Volume**: Number of invoices generated per run
- **Failed Invoices**: Track invoices that failed to generate
- **Credit Notes**: Monitor credit note creation frequency

### Logging Configuration

**Recommended Log Levels (Production):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "TentMan.Application.Billing": "Information",
      "TentMan.Application.BackgroundJobs": "Information",
      "Hangfire": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Application Insights Integration (Azure)

Add Application Insights SDK to track:
- Request telemetry
- Dependency telemetry (SQL queries)
- Exception telemetry
- Custom events (invoice generation, job execution)

**Configuration:**
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "<your-key>"
  }
}
```

### Alert Configuration

Set up alerts for:

1. **Failed Invoice Runs**
   - Trigger: InvoiceRun status = Failed
   - Action: Email/SMS to ops team

2. **High Error Rate**
   - Trigger: Error rate > 5% over 5 minutes
   - Action: Create incident ticket

3. **Background Job Failures**
   - Trigger: Hangfire job failed 3 consecutive times
   - Action: Email notification

4. **Database Issues**
   - Trigger: Database connection failures
   - Action: Immediate escalation

---

## Security Considerations

### Authorization Policies

The billing engine enforces strict authorization:

**Invoice Management:**
- View invoices: `SuperAdmin`, `Administrator`, `Manager`, or tenant associated with lease
- Issue invoices: `SuperAdmin`, `Administrator`, `Manager`
- Void invoices: `SuperAdmin`, `Administrator`

**Billing Settings:**
- View: `SuperAdmin`, `Administrator`, `Manager`
- Modify: `SuperAdmin`, `Administrator`

**Hangfire Dashboard:**
- Access: `SuperAdmin`, `Administrator` only

### Sensitive Data Handling

**What is Logged:**
- Invoice IDs and numbers
- Lease IDs
- Organization IDs
- Operation timestamps
- Success/failure status

**What is NOT Logged:**
- Specific invoice amounts in detailed logs
- Tenant personal information
- Payment details

**Data Masking:**
When returning invoice data to tenants, ensure sensitive information is properly scoped by lease access.

### SQL Injection Prevention

The application uses Entity Framework Core with parameterized queries throughout. No dynamic SQL is used in billing operations.

**Safe Query Pattern:**
```csharp
// SAFE: Parameterized query via EF Core
var invoices = await _context.Invoices
    .Where(i => i.LeaseId == leaseId && i.Status == status)
    .ToListAsync();
```

**Avoid:**
```csharp
// UNSAFE: Never use string interpolation for queries
// This pattern is NOT used in the codebase
await _context.Database.ExecuteSqlRawAsync($"SELECT * FROM Invoices WHERE LeaseId = '{leaseId}'");
```

### Audit Trail

All billing operations are automatically audited via the `AuditLoggingInterceptor`:

**Tracked Operations:**
- Invoice issuance
- Invoice voiding
- Credit note creation
- Billing setting modifications
- Recurring charge updates

**Audit Log Fields:**
- Entity type and ID
- Operation (Create, Update, Delete)
- User ID
- Timestamp
- Changed values (before/after)

**Querying Audit Logs:**
```sql
SELECT * FROM AuditLogs 
WHERE EntityType IN ('Invoice', 'CreditNote', 'LeaseBillingSetting')
ORDER BY CreatedAtUtc DESC;
```

---

## Rollback Procedures

### Scenario 1: Migration Needs Rollback

If issues are discovered after migration:

**Step 1: Stop Application**
```bash
# Azure App Service
az webapp stop --resource-group <rg-name> --name <app-name>

# IIS
Stop-Website -Name "TentMan"

# Docker
docker stop tentman-api
```

**Step 2: Restore Database Backup**
```sql
-- Restore from backup
RESTORE DATABASE TentManDb 
FROM DISK = 'C:\Backups\TentManDb_PreBilling_20260112.bak'
WITH REPLACE;
```

**Step 3: Deploy Previous Application Version**

Deploy the application version before billing engine integration.

### Scenario 2: Data Issues Discovered

If data inconsistencies are found but migration is successful:

**Option A: Fix Data with Script**
```sql
-- Example: Fix incorrect billing settings
UPDATE LeaseBillingSettings
SET BillingDay = 1, ProrationMethod = 1
WHERE CreatedBy = 'Migration' AND BillingDay > 28;
```

**Option B: Rollback Specific Migration**
```bash
# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> --startup-project src/TentMan.Api
```

### Scenario 3: Hangfire Job Issues

If background jobs are causing problems:

**Step 1: Disable Jobs**
```csharp
// In Program.cs, comment out job registration
// RecurringJob.AddOrUpdate<MonthlyRentGenerationJob>(...);
```

**Step 2: Delete Jobs from Hangfire**
```sql
-- Clear recurring jobs
DELETE FROM Hangfire.Set WHERE [Key] LIKE 'recurring-jobs%';
```

**Step 3: Redeploy with Jobs Disabled**

Investigate and fix job issues, then re-enable.

---

## Post-Deployment Verification

### Immediate Checks (First 15 Minutes)

1. **Application Health**
   ```bash
   curl https://your-api.com/health
   # Expected: HTTP 200 with "Healthy" status
   ```

2. **Hangfire Dashboard**
   - Navigate to `/hangfire`
   - Verify 2 recurring jobs are registered
   - Check server is processing jobs

3. **Database Verification**
   ```sql
   -- Count billing records
   SELECT 
       'ChargeTypes' as TableName, COUNT(*) as RecordCount FROM ChargeTypes WHERE IsSystemDefined = 1
   UNION ALL
   SELECT 'LeaseBillingSettings', COUNT(*) FROM LeaseBillingSettings
   UNION ALL
   SELECT 'Hangfire Jobs', COUNT(*) FROM Hangfire.Job;
   ```

4. **API Endpoints**
   ```bash
   # Test billing endpoints (requires authentication)
   curl -H "Authorization: Bearer <token>" https://your-api.com/api/v1/billing/charge-types
   curl -H "Authorization: Bearer <token>" https://your-api.com/api/v1/billing/invoices
   ```

### First 24 Hours Monitoring

- [ ] Monitor application logs for errors
- [ ] Check Hangfire dashboard for job execution
- [ ] Monitor database performance
- [ ] Verify no spike in error rates
- [ ] Check alert system is working

### First Week Monitoring

- [ ] Review invoice generation patterns
- [ ] Monitor background job execution times
- [ ] Analyze database query performance
- [ ] Gather user feedback
- [ ] Review security audit logs

---

## Troubleshooting

### Issue: Migration Fails

**Symptoms:** Migration command fails with error

**Diagnosis:**
```bash
# Check migration status
dotnet ef migrations list --startup-project src/TentMan.Api
```

**Solutions:**
1. Check database connection string
2. Verify database user has DDL permissions
3. Check for schema conflicts
4. Review error message for specific SQL error

### Issue: Hangfire Jobs Not Running

**Symptoms:** Jobs shown as "Enqueued" but never execute

**Diagnosis:**
```sql
-- Check Hangfire servers
SELECT * FROM Hangfire.Server;

-- Check job queue
SELECT * FROM Hangfire.Job WHERE StateName = 'Enqueued';
```

**Solutions:**
1. Verify Hangfire server is running (check logs)
2. Increase worker count if jobs are queued
3. Check for database deadlocks
4. Restart application to restart Hangfire server

### Issue: Invoice Generation Fails

**Symptoms:** MonthlyRentGenerationJob fails

**Diagnosis:**
```csharp
// Check logs for specific error
// Look for: "Failed to generate invoice for lease {LeaseId}"
```

**Common Causes:**
1. Missing LeaseBillingSetting for lease
2. Invalid lease data (e.g., missing rent amount)
3. Database connection issues
4. Concurrency conflicts

**Solutions:**
1. Add missing billing settings
2. Fix invalid lease data
3. Check database connectivity
4. Review retry policy

### Issue: High Memory Usage

**Symptoms:** Application consuming excessive memory

**Diagnosis:**
- Monitor Hangfire worker count
- Check for memory leaks in batch operations
- Review invoice generation batch size

**Solutions:**
1. Reduce Hangfire worker count
2. Implement pagination for large result sets
3. Add garbage collection hints for large operations
4. Increase server memory capacity

### Issue: Slow Database Queries

**Symptoms:** Slow response times for billing operations

**Diagnosis:**
```sql
-- Find slow queries
SELECT TOP 10 
    qs.total_elapsed_time / qs.execution_count as avg_elapsed_time,
    qs.execution_count,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1, 
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) as query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.text LIKE '%Invoice%' OR qt.text LIKE '%Billing%'
ORDER BY avg_elapsed_time DESC;
```

**Solutions:**
1. Add missing indexes on billing tables
2. Optimize queries with execution plans
3. Consider database scaling
4. Implement caching for frequently accessed data

---

## Related Documentation

- **[BILLING_ENGINE.md](BILLING_ENGINE.md)** - Billing engine architecture and features
- **[BACKGROUND_JOBS.md](BACKGROUND_JOBS.md)** - Background job configuration
- **[DATABASE_GUIDE.md](DATABASE_GUIDE.md)** - Database management guide
- **[SECURITY.md](SECURITY.md)** - Security policies and controls
- **[API_GUIDE.md](API_GUIDE.md)** - API reference and authentication

---

## Support & Escalation

For deployment issues or questions:

1. Check this documentation first
2. Review application logs and Hangfire dashboard
3. Consult the troubleshooting section
4. Escalate to development team if unresolved

**Emergency Rollback Contact:** [Define emergency contact procedure]

---

**Document Version:** 1.0  
**Last Updated:** 2026-01-12  
**Maintainer:** TentMan Development Team
