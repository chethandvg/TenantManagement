# AdminApi AppHost Integration - Verification Checklist

## ✅ Configuration Verification

### 1. AppHost Configuration

- [x] **AdminApi added to both Docker and Local modes**
  - `builder.AddProject<Projects.Archu_AdminApi>("admin-api")`
  
- [x] **Database reference added (Docker mode)**
  - `.WithReference(sql)` - Shares database with main API
  
- [x] **Environment configured**
  - `.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")`
  
- [x] **External endpoints enabled**
  - `.WithExternalHttpEndpoints()` - Makes API accessible from browser
  
- [x] **Scalar documentation enabled**
  - `.WithScalar()` - Interactive API docs

### 2. AdminApi appsettings.json

- [x] **Logging configuration**
  ```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
  ```

- [x] **Connection strings placeholders**
  ```json
  "ConnectionStrings": {
    "archudb": "",
    "Sql": ""
  }
  ```

- [x] **JWT configuration**
  ```json
  "Jwt": {
    "Issuer": "https://localhost:7002",
    "Audience": "https://localhost:7002",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
  ```

### 3. AdminApi appsettings.Development.json

- [x] **Enhanced logging for development**
  ```json
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "Archu": "Debug"
    }
  }
  ```

- [x] **Local database connection string**
  ```json
  "ConnectionStrings": {
    "archudb": "Server=localhost;Database=ArchuDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
  ```

- [x] **JWT configuration (same as production structure)**

## 🔍 Key Changes Made

### From Previous Configuration
```csharp
// ❌ BEFORE: Incomplete configuration
builder.AddProject<Projects.Archu_AdminApi>("archu-adminapi");
```

### To Current Configuration
```csharp
// ✅ AFTER: Complete configuration with all required settings
var adminApi = builder.AddProject<Projects.Archu_AdminApi>("admin-api")
    .WithReference(sql)  // Share database
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()  // Enable external access
    .WithScalar();  // Enable API docs
```

## 🎯 Why These Changes Matter

### 1. Database Sharing (`.WithReference(sql)`)
**Problem**: Without this, AdminApi wouldn't know which database to use in Docker mode.

**Solution**: AdminApi now receives the same connection string as the main API.

**Result**: Both APIs work with the same database, ensuring data consistency.

### 2. External HTTP Endpoints (`.WithExternalHttpEndpoints()`)
**Problem**: Without this, the API is only accessible within the Aspire network.

**Solution**: Makes the API accessible from browser, Postman, curl, etc.

**Result**: Can access https://localhost:7002 from any HTTP client.

### 3. Scalar Documentation (`.WithScalar()`)
**Problem**: No easy way to explore and test API endpoints.

**Solution**: Adds Scalar UI at https://localhost:7002/scalar/v1

**Result**: Interactive API documentation and testing interface.

### 4. Environment Configuration
**Problem**: AdminApi might not load the correct configuration.

**Solution**: Explicitly sets Development environment.

**Result**: Uses appsettings.Development.json and enables detailed error messages.

## 🧪 Testing Checklist

### Pre-Flight Checks

- [ ] **Build Solution**
  ```bash
  dotnet build
  ```
  Should complete without errors.

- [ ] **Verify Project References**
  ```bash
  dotnet list src/Archu.AppHost/Archu.AppHost.csproj reference
  ```
  Should include Archu.AdminApi project.

### Startup Tests

- [ ] **Start AppHost**
  ```bash
  cd src/Archu.AppHost
  dotnet run
  ```

- [ ] **Verify Aspire Dashboard Opens**
  - Dashboard URL shown in console (e.g., http://localhost:15001)
  - Should show 3 resources: sql, api, admin-api

- [ ] **Verify Services Start**
  - Main API: https://localhost:7001
  - Admin API: https://localhost:7002
  - All should show as "Running" in Aspire Dashboard

### Database Verification

- [ ] **Docker Mode: Verify SQL Server Container**
  ```bash
  docker ps | grep sql
  ```
  Should show running SQL Server container.

- [ ] **Verify Database Created**
  - Check Aspire Dashboard logs for migration messages
  - Or connect to database and verify `ArchuDb` exists

- [ ] **Verify Connection Strings**
  - Main API logs should show connection string
  - Admin API logs should show the SAME connection string

### API Endpoint Tests

- [ ] **Main API Health Check**
  ```bash
  curl https://localhost:7001/health -k
  ```

- [ ] **Admin API Health Check**
  ```bash
  curl https://localhost:7002/health -k
  ```

- [ ] **Main API Scalar Docs**
  - Open: https://localhost:7001/scalar/v1
  - Should show API documentation

- [ ] **Admin API Scalar Docs**
  - Open: https://localhost:7002/scalar/v1
  - Should show Admin API documentation
  - Should include Initialization endpoint

### Initialization Test

- [ ] **Initialize System**
  ```bash
  curl -X POST https://localhost:7002/api/v1/admin/initialization/initialize \
    -H "Content-Type: application/json" \
    -d '{"userName":"superadmin","email":"admin@test.com","password":"TestPass123!"}' -k
  ```
  Should return success with created roles and user.

- [ ] **Verify Roles Created**
  ```bash
  # Login first to get token
  TOKEN=$(curl -X POST https://localhost:7001/api/v1/authentication/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@test.com","password":"TestPass123!"}' -k \
    | jq -r '.data.accessToken')
  
  # Get roles
  curl https://localhost:7002/api/v1/admin/roles \
    -H "Authorization: Bearer $TOKEN" -k
  ```
  Should return 5 roles: Guest, User, Manager, Administrator, SuperAdmin.

### Cross-API Authentication Test

- [ ] **Get Token from Main API**
  ```bash
  curl -X POST https://localhost:7001/api/v1/authentication/login \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@test.com","password":"TestPass123!"}' -k
  ```
  Save the access token.

- [ ] **Use Token on Admin API**
  ```bash
  curl https://localhost:7002/api/v1/admin/users \
    -H "Authorization: Bearer YOUR_TOKEN_HERE" -k
  ```
  Should return list of users (at least the super admin).

## 🚨 Common Issues & Solutions

### Issue 1: "Project does not exist" error

**Symptom**: Build error referencing `Projects.Archu_AdminApi`

**Solution**: 
1. Clean solution: `dotnet clean`
2. Rebuild: `dotnet build`
3. Ensure AdminApi.csproj is in the correct location

### Issue 2: AdminApi starts but can't connect to database

**Docker Mode:**
- Verify `WithReference(sql)` is present
- Check Aspire Dashboard for SQL container status
- View AdminApi logs in Aspire Dashboard

**Local Mode:**
- Verify connection string in appsettings.Development.json
- Ensure SQL Server is running locally
- Check database name matches

### Issue 3: Port conflicts

**Symptom**: Error "Address already in use"

**Solution**:
1. Close other applications using ports 7001, 7002
2. Or modify ports in launchSettings.json
3. Or let Aspire assign dynamic ports (remove explicit port config)

### Issue 4: JWT tokens don't work on Admin API

**Check:**
1. Same JWT secret in both APIs
2. Same Issuer/Audience configuration
3. Token not expired
4. User has required role (SuperAdmin or Administrator)

### Issue 5: Initialization endpoint returns "already initialized"

**Explanation**: This is correct behavior if users already exist.

**Solution**:
- To re-initialize: Drop and recreate database
- Or use admin endpoints to create additional users/roles

## 📊 Success Criteria

### ✅ All checks passed when:

1. **Build succeeds** without errors
2. **AppHost starts** all three services (sql, api, admin-api)
3. **Aspire Dashboard** shows all services as "Running"
4. **Health endpoints** return 200 OK
5. **Scalar docs** accessible for both APIs
6. **Initialization endpoint** successfully creates roles and super admin
7. **Authentication works** between both APIs
8. **Database is shared** (user created in AdminApi can login via Main API)

## 🔄 Ongoing Validation

### During Development

- Monitor Aspire Dashboard for service health
- Check logs for any connection errors
- Verify database changes appear in both APIs
- Test JWT token usage across APIs

### Before Committing Code

- [ ] All tests pass
- [ ] Build succeeds
- [ ] No console errors when starting AppHost
- [ ] Documentation updated (if needed)
- [ ] appsettings files properly configured

---

**Last Updated**: 2025-01-22  
**Checklist Version**: 1.0  
**Maintainer**: Archu Development Team
