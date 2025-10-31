# Archu - Getting Started Guide

Complete guide to set up and start using Archu from scratch.

---

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for Aspire AppHost)
- SQL Server or Docker
- Visual Studio 2022 / Rider / VS Code

---

## 🚀 Quick Start (5 Minutes)

### Step 1: Clone and Run

```bash
git clone https://github.com/chethandvg/archu.git
cd archu/src/Archu.AppHost
dotnet run
```

The Aspire Dashboard will open automatically with all services.

### Step 2: Configure JWT Authentication

**Windows:**
```powershell
cd ../Archu.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKeyForJWTTokenGeneration1234567890"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

**Linux/macOS:**
```bash
cd ../Archu.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKeyForJWTTokenGeneration1234567890"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

### Step 3: Enable Database Seeding (Optional)

```bash
dotnet user-secrets set "DatabaseSeeding:Enabled" "true"
dotnet user-secrets set "DatabaseSeeding:SeedRoles" "true"
dotnet user-secrets set "DatabaseSeeding:SeedAdminUser" "true"
dotnet user-secrets set "DatabaseSeeding:AdminEmail" "admin@archu.com"
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "Admin@123"
```

### Step 4: Access the Application

- **Main API**: https://localhost:7123
- **Admin API**: https://localhost:7290
- **Main API Docs**: https://localhost:7123/scalar/v1
- **Admin API Docs**: https://localhost:7290/scalar/v1
- **Aspire Dashboard**: Check console output for URL

---

## 🔐 JWT Authentication Setup (Detailed)

### Manual JWT Configuration

The repository no longer includes automated setup scripts, so use the manual commands below (they mirror the quick start instructions above).

**Windows (PowerShell):**
```powershell
cd ../Archu.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKeyForJWTTokenGeneration1234567890"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

**Linux/macOS (bash):**
```bash
cd ../Archu.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKeyForJWTTokenGeneration1234567890"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

### What Gets Configured

These commands:
- ✅ Initialize User Secrets
- ✅ Store a secure 256-bit JWT secret
- ✅ Configure JWT settings (Issuer, Audience, Expiration)

### Verify Configuration

```bash
dotnet user-secrets list
```

Expected output:
```
Jwt:Secret = <your-secure-secret>
Jwt:Issuer = https://localhost:7123
Jwt:Audience = https://localhost:7123
Jwt:AccessTokenExpirationMinutes = 60
Jwt:RefreshTokenExpirationDays = 7
```

### Manual Secret Generation

If you prefer to generate your own secret:

**PowerShell:**
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host $secret
```

**Linux/macOS:**
```bash
openssl rand -base64 32
```

---

## 🗄️ Database Seeding Setup

### What is Database Seeding?

Automatic initialization of:
- 5 system roles (Guest, User, Manager, Administrator, SuperAdmin)
- 1 admin user with credentials you specify
- Role assignment for the admin user

### Automated Setup

Enable seeding in User Secrets:

```bash
cd src/Archu.Api
dotnet user-secrets set "DatabaseSeeding:Enabled" "true"
dotnet user-secrets set "DatabaseSeeding:SeedRoles" "true"
dotnet user-secrets set "DatabaseSeeding:SeedAdminUser" "true"
dotnet user-secrets set "DatabaseSeeding:AdminEmail" "admin@archu.com"
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "Admin@123"
dotnet user-secrets set "DatabaseSeeding:AdminRoles" "SuperAdmin,Administrator,User"
```

### Verify Seeding

On application startup, check logs for:
```
info: Starting database seeding process...
info: Seeding roles...
info: Seeding admin user...
info: Database seeding completed successfully
```

### Default Credentials

After seeding completes:
- **Email**: admin@archu.com
- **Password**: Admin@123
- **Roles**: SuperAdmin, Administrator, User

⚠️ **IMPORTANT**: Change these credentials in production!

---

## 🔧 Admin API Setup

### Configure JWT for Admin API

The Admin API needs the **SAME JWT secret** as the Main API for tokens to work across both APIs.

Follow the same manual process for the Admin API:

**Windows (PowerShell):**
```powershell
cd ../Archu.AdminApi
dotnet user-secrets init
dotnet user-secrets list --project ../Archu.Api
# Copy the Jwt:Secret value from the output above and paste it below
dotnet user-secrets set "Jwt:Secret" "<paste-secret-from-main-api>"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

**Linux/macOS (bash):**
```bash
cd ../Archu.AdminApi
dotnet user-secrets init
dotnet user-secrets list --project ../Archu.Api
# Copy the Jwt:Secret value from the output above and paste it below
dotnet user-secrets set "Jwt:Secret" "<paste-secret-from-main-api>"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

⚠️ **CRITICAL**: Both APIs MUST use the **EXACT SAME** JWT secret.

### Initialize the Admin System (One Time Only)

Before using admin features, initialize the system:

```bash
# Start AdminApi
cd Archu.AdminApi
dotnet run

# Initialize (creates roles and super admin)
curl -X POST https://localhost:7290/api/v1/admin/initialization/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "superadmin",
    "email": "admin@yourcompany.com",
    "password": "YourSecurePassword123!"
  }'
```

---

## 🧪 Testing Your Setup

### Test Authentication

1. **Navigate to API Documentation**: https://localhost:7123/scalar/v1

2. **Login**:
```http
POST /api/v1/authentication/login
Content-Type: application/json

{
  "email": "admin@archu.com",
  "password": "Admin@123"
}
```

3. **Copy Access Token** from the response

4. **Authenticate in Scalar UI**:
   - Click "Authorize"
   - Paste token: `Bearer <your-access-token>`

5. **Test Protected Endpoints**:
   - Try GET /api/v1/products
   - Should return 200 OK with product list

### Test Admin API

1. **Get JWT Token** from Main API (use login endpoint above)

2. **Navigate to Admin API Docs**: https://localhost:7290/scalar/v1

3. **Test User Management**:
```bash
TOKEN="your-access-token-here"

# List all users
curl -X GET https://localhost:7290/api/v1/admin/users \
  -H "Authorization: Bearer $TOKEN"

# Create a new user
curl -X POST https://localhost:7290/api/v1/admin/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "testuser",
    "email": "test@example.com",
    "password": "Test@123",
    "emailConfirmed": false
  }'
```

---

## 📝 Using HTTP Request Files

Archu includes ready-to-use HTTP request files for testing:

### Main API Requests

**File**: `src/Archu.Api/Archu.Api.http`
**Requests**: 40+ examples

**Setup**:
1. Open `Archu.Api.http` in Visual Studio
2. Update the `jwt_token` variable with your token
3. Click "Send Request" next to any request

**Categories**:
- Health checks
- Authentication (register, login, refresh)
- Products (CRUD operations)
- Account management

### Admin API Requests

**File**: `Archu.AdminApi/Archu.AdminApi.http`
**Requests**: 31 examples

**Setup**:
1. Open `Archu.AdminApi.http` in Visual Studio
2. Update the `jwt_token` variable with your token
3. Click "Send Request" next to any request

**Categories**:
- System initialization
- User management (list, create, update, delete)
- Role management (list, create, update, delete)
- User role assignments (assign, remove, list)

---

## 🛡️ Security Best Practices

### Development Environment

✅ **Use User Secrets** for JWT configuration
```bash
dotnet user-secrets set "Jwt:Secret" "<secure-secret>"
```

✅ **Enable HTTPS** (default in .NET)

✅ **Use strong passwords** for test accounts

✅ **Limit access tokens** to 15-60 minutes

✅ **Enable refresh tokens** for seamless re-authentication

### Production Environment

✅ **Use Azure Key Vault** for secrets (see [Authentication Guide](AUTHENTICATION_GUIDE.md))

✅ **Change default credentials** immediately

✅ **Limit SuperAdmin accounts** to 1-2 trusted users

✅ **Monitor admin actions** through logs

✅ **Use HTTPS only** with valid certificates

✅ **Enable rate limiting** for authentication endpoints

---

## 🚨 Troubleshooting

### "JWT Secret is not configured"

**Cause**: Missing JWT configuration in User Secrets

**Fix**:
```bash
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKeyForJWTTokenGeneration1234567890"
```

### "JWT Secret must be at least 32 characters"

**Cause**: Secret is too short

**Fix**: Generate a secure 256-bit secret (see Manual Secret Generation above)

### Database Seeding Not Running

**Check 1**: Verify seeding is enabled
```bash
dotnet user-secrets list | grep DatabaseSeeding:Enabled
```

**Check 2**: Check startup logs for seeding messages

**Fix**: Enable seeding
```bash
dotnet user-secrets set "DatabaseSeeding:Enabled" "true"
```

### 401 Unauthorized on Admin API

**Cause**: Token from Main API not working on Admin API

**Fix**: Ensure both APIs use the SAME JWT secret
```bash
# Check Main API
cd src/Archu.Api
dotnet user-secrets list | grep Jwt:Secret

# Check Admin API
cd ../../Archu.AdminApi
dotnet user-secrets list | grep Jwt:Secret

# They should be identical
```

### Cannot Connect to Database

**Cause**: SQL Server not running or connection string incorrect

**Fix**: 
1. Ensure SQL Server is running
2. Check connection string in appsettings.json
3. For Aspire, check dashboard for SQL Server container status

---

## 📊 Response Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **200** | OK | Successful operation |
| **201** | Created | Resource created successfully |
| **400** | Bad Request | Validation error, invalid data |
| **401** | Unauthorized | Missing/expired/invalid token |
| **403** | Forbidden | Insufficient permissions |
| **404** | Not Found | Resource doesn't exist |
| **500** | Internal Server Error | Server error (check logs) |

---

## ✅ Setup Checklist

- [ ] .NET 9 SDK installed
- [ ] Repository cloned
- [ ] JWT secrets configured (Main API)
- [ ] JWT secrets configured (Admin API, same as Main)
- [ ] Database seeding enabled (optional)
- [ ] Application runs successfully
- [ ] Can login via Main API
- [ ] Token works on protected endpoints
- [ ] Token works on Admin API
- [ ] Scalar UI accessible
- [ ] HTTP request files work

---

## 🎯 Next Steps

After completing this guide:

1. ✅ **Explore the API** - Use Scalar UI to test endpoints
2. ✅ **Read Architecture Guide** - [ARCHITECTURE.md](ARCHITECTURE.md)
3. ✅ **Learn Development Workflow** - [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)
4. ✅ **Configure Password Policies** - [PASSWORD_SECURITY_GUIDE.md](PASSWORD_SECURITY_GUIDE.md)
5. ✅ **Set up Authorization** - [AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)

---

## 📚 Related Documentation

- **[README.md](../README.md)** - Project overview
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture
- **[API_GUIDE.md](API_GUIDE.md)** - Complete API reference
- **[AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)** - JWT and authentication details
- **[DATABASE_GUIDE.md](DATABASE_GUIDE.md)** - Database and seeding

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
