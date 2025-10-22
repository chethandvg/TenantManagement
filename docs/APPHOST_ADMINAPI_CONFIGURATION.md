# AppHost Configuration for AdminApi

## Overview

This document explains how the AdminApi is integrated into the .NET Aspire AppHost orchestration.

## Configuration Details

### Shared Database Architecture

Both `Archu.Api` and `Archu.AdminApi` share the same database instance:

```
┌─────────────────┐
│   SQL Server    │
│    (Docker or   │
│   Local Server) │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
┌───▼───┐ ┌──▼────────┐
│  API  │ │ AdminApi  │
└───────┘ └───────────┘
```

**Why share the database?**
- ✅ Admin operations (creating users/roles) are immediately available to the main API
- ✅ Single source of truth for user authentication
- ✅ No data synchronization needed
- ✅ Consistent data across both APIs

### AppHost Configuration

The AppHost is configured with two modes:

#### Docker Database Mode (Default)
```csharp
const bool useDockerDatabase = true;

var sql = builder.AddSqlServer("sql").WithDataVolume()
    .AddDatabase("archudb");

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sql)  // Injects Docker connection string
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithScalar();

var adminApi = builder.AddProject<Projects.Archu_AdminApi>("admin-api")
    .WithReference(sql)  // Same database as main API
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithScalar();
```

**Features:**
- ✅ Automatic SQL Server container provisioning
- ✅ Persistent volume for data
- ✅ Connection string automatically injected by Aspire
- ✅ Both APIs share the same database instance

#### Local SQL Server Mode
```csharp
const bool useDockerDatabase = false;

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithScalar();

var adminApi = builder.AddProject<Projects.Archu_AdminApi>("admin-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithScalar();
```

**Features:**
- ✅ Uses local SQL Server installation
- ✅ Connection strings from appsettings.Development.json
- ✅ Both APIs use the same connection string (same database)

## Service Configuration

### Main API (Archu.Api)
- **Service Name**: `api`
- **Default Port**: 7001 (HTTPS), 5000 (HTTP)
- **Scalar Docs**: `https://localhost:7001/scalar/v1`
- **Purpose**: Business API endpoints (products, authentication)

### Admin API (Archu.AdminApi)
- **Service Name**: `admin-api`
- **Default Port**: 7002 (HTTPS), 5001 (HTTP)
- **Scalar Docs**: `https://localhost:7002/scalar/v1`
- **Purpose**: Administrative endpoints (users, roles, initialization)

## Aspire Features Enabled

Both APIs have the following Aspire features enabled:

### 1. External HTTP Endpoints
```csharp
.WithExternalHttpEndpoints()
```
- Makes the API accessible from outside the Aspire network
- Required for browser/Postman access
- Enables local development testing

### 2. Scalar API Documentation
```csharp
.WithScalar()
```
- Adds Scalar UI for API documentation
- Interactive API testing interface
- Automatically generated from OpenAPI specs

### 3. Environment Configuration
```csharp
.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
```
- Sets development environment
- Enables detailed error messages
- Uses appsettings.Development.json

### 4. Service Defaults
Both APIs use `AddServiceDefaults()` which provides:
- ✅ OpenTelemetry integration
- ✅ Health checks
- ✅ Service discovery
- ✅ Structured logging

## Connection String Injection

### Docker Mode
When using Docker database, Aspire automatically injects the connection string:

**Environment Variable Injected:**
```
ConnectionStrings__Sql=Server=sql;Database=archudb;...
```

Both APIs receive the same connection string, ensuring they use the same database.

### Local Mode
When using local SQL Server, both APIs read from their appsettings.Development.json:

**Archu.AdminApi/appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "archudb": "Server=localhost;Database=ArchuDb;..."
  }
}
```

**Important**: Both APIs must use the **same database name** (`ArchuDb`) in their connection strings.

## Running the Application

### Start All Services
```bash
cd src/Archu.AppHost
dotnet run
```

This starts:
1. **SQL Server** (if Docker mode)
2. **Archu.Api** on ports 7001 (HTTPS) / 5000 (HTTP)
3. **Archu.AdminApi** on ports 7002 (HTTPS) / 5001 (HTTP)
4. **Aspire Dashboard** (shows all services and logs)

### Accessing Services

| Service | URL | Purpose |
|---------|-----|---------|
| **Main API** | https://localhost:7001 | Business endpoints |
| **Main API Docs** | https://localhost:7001/scalar/v1 | API documentation |
| **Admin API** | https://localhost:7002 | Admin endpoints |
| **Admin API Docs** | https://localhost:7002/scalar/v1 | Admin API docs |
| **Aspire Dashboard** | http://localhost:15XXX | Monitor all services |

**Note**: Aspire Dashboard port varies - check console output for exact URL.

## Initialization Workflow

### First Time Setup

1. **Start AppHost** (this creates/migrates the database)
2. **Initialize System** via AdminApi:
   ```bash
   curl -X POST https://localhost:7002/api/v1/admin/initialization/initialize \
     -H "Content-Type: application/json" \
     -d '{"userName":"superadmin","email":"admin@company.com","password":"SecurePass123!"}'
   ```
3. **Login** via Main API to get token:
   ```bash
   curl -X POST https://localhost:7001/api/v1/authentication/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@company.com","password":"SecurePass123!"}'
   ```
4. **Use Admin Endpoints** with the token

## Database Migrations

Both APIs use Entity Framework Core migrations from `Archu.Infrastructure`.

### Applying Migrations

**Option 1: Automatic (Development)**
- Migrations are applied automatically on startup in Development mode

**Option 2: Manual**
```bash
cd src/Archu.Infrastructure
dotnet ef database update --startup-project ../Archu.Api
```

**Important**: Migrations only need to be run once since both APIs share the same database.

## Security Considerations

### JWT Secret Configuration

Both APIs need JWT secrets configured:

**For Development:**
```bash
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "your-secret-key-here"

cd ../../Archu.AdminApi
dotnet user-secrets set "Jwt:Secret" "your-secret-key-here"
```

**Important**: Use the **same JWT secret** for both APIs so tokens are interchangeable.

### Cross-API Authentication

Since both APIs share:
- ✅ Same database (users, roles)
- ✅ Same JWT secret
- ✅ Same JWT configuration

A token issued by the Main API works on Admin API endpoints and vice versa.

## Troubleshooting

### Issue: Admin API cannot connect to database

**Docker Mode:**
- Ensure `WithReference(sql)` is present for AdminApi
- Check Aspire Dashboard for SQL Server container status

**Local Mode:**
- Verify connection string in `appsettings.Development.json`
- Ensure SQL Server is running
- Test connection string manually

### Issue: Ports already in use

Default ports:
- Main API: 7001 (HTTPS), 5000 (HTTP)
- Admin API: 7002 (HTTPS), 5001 (HTTP)

**Solution**: Change ports in `launchSettings.json` or let Aspire assign dynamic ports.

### Issue: Database not shared between APIs

**Check:**
1. Both APIs use the same connection string
2. Both APIs reference the same `sql` resource (Docker mode)
3. Database name is identical in both connection strings (Local mode)

### Issue: JWT tokens not working across APIs

**Solution**: Ensure both APIs have:
- ✅ Same JWT secret in user secrets
- ✅ Same Issuer and Audience in appsettings.json
- ✅ Same token expiration settings

## Best Practices

### Development
- ✅ Use Docker mode for consistency
- ✅ Use Aspire Dashboard to monitor both APIs
- ✅ Keep JWT secrets in sync using user secrets
- ✅ Run migrations from one API only

### Production
- ✅ Use managed database service (Azure SQL, AWS RDS)
- ✅ Store JWT secrets in Azure Key Vault or similar
- ✅ Use environment variables for connection strings
- ✅ Consider separate deployments but shared database

## Architecture Benefits

### Why This Design?

1. **Separation of Concerns**
   - Admin operations in separate API
   - Business logic in main API
   - Clear responsibility boundaries

2. **Security**
   - Admin endpoints can have stricter security
   - Can restrict Admin API to internal network only
   - Independent rate limiting and monitoring

3. **Scalability**
   - APIs can be scaled independently
   - Admin API typically needs fewer resources
   - Main API can handle higher traffic

4. **Maintainability**
   - Clear separation makes code easier to understand
   - Changes to admin features don't affect main API
   - Independent deployment possible

## Next Steps

1. **Run AppHost** to verify both APIs start correctly
2. **Check Aspire Dashboard** to see service health
3. **Initialize System** using Admin API
4. **Test Authentication** flow between both APIs
5. **Verify Database Sharing** by creating a user in Admin API and logging in via Main API

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
