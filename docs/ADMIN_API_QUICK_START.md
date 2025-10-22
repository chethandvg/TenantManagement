# Admin API Quick Start

This guide helps you initialize and use the Archu Admin API for user and role management.

## 📋 Overview

The Admin API provides endpoints for:
- System initialization (one-time setup)
- User management
- Role management  
- User-role assignments

**For complete documentation**, see: [Archu.AdminApi/README.md](../Archu.AdminApi/README.md)

## 🚀 Quick Start

### Step 0: Configure JWT Secrets (Required)

**IMPORTANT**: Before running the Admin API, you must configure JWT secrets.

#### Option A: Setup Both APIs at Once (Recommended)

Run this script from the repository root to configure both Main API and Admin API with the same JWT secret:

**Windows (PowerShell):**
```powershell
.\scripts\setup-jwt-secrets-all.ps1
```

**Linux/macOS:**
```bash
chmod +x ./scripts/setup-jwt-secrets-all.sh
./scripts/setup-jwt-secrets-all.sh
```

#### Option B: Manual Setup

If you prefer to set up manually or use your own secret:

```bash
# 1. Navigate to Main API
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "your-64-character-secret-here"

# 2. Navigate to Admin API
cd ../../Archu.AdminApi
dotnet user-secrets set "Jwt:Secret" "your-64-character-secret-here"
```

**CRITICAL**: Both APIs MUST use the **EXACT SAME** JWT secret for tokens to work across both APIs.

### Step 1: Initialize the System (One Time Only)

Before using any admin features, you must initialize the system:

```bash
# Start AdminApi
cd Archu.AdminApi
dotnet run

# Initialize system (creates roles and super admin)
curl -X POST https://localhost:7002/api/v1/admin/initialization/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "superadmin",
    "email": "admin@yourcompany.com",
    "password": "YourSecurePassword123!"
  }'
```

**What this creates:**
- ✅ 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
- ✅ 1 super admin user with your credentials
- ✅ SuperAdmin role assigned to the user

### Step 2: Login to Get Access Token

```bash
# Login with super admin credentials
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@yourcompany.com",
    "password": "YourSecurePassword123!"
  }'
```

Save the `accessToken` from the response.

### Step 3: Use Admin Endpoints

All admin endpoints require the JWT token:

```bash
TOKEN="your-access-token-here"

# List all users
curl -X GET https://localhost:7002/api/v1/admin/users \
  -H "Authorization: Bearer $TOKEN"

# Create a new user
curl -X POST https://localhost:7002/api/v1/admin/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "newuser",
    "email": "newuser@example.com",
    "password": "SecurePassword123!",
    "emailConfirmed": false
  }'

# List all roles
curl -X GET https://localhost:7002/api/v1/admin/roles \
  -H "Authorization: Bearer $TOKEN"

# Assign role to user
curl -X POST https://localhost:7002/api/v1/admin/userroles/assign \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-guid",
    "roleId": "role-guid"
  }'
```

## 🔒 Security Notes

- **Store super admin credentials securely** - Never commit to source control
- **Change default password** after initialization
- **Limit SuperAdmin accounts** - Only create when necessary
- **Use Administrator role** for most admin tasks
- **Monitor admin actions** - All operations are logged
- **JWT secrets must match** - Both APIs need the same secret for token interoperability

## 🚨 Troubleshooting

### Error: "JWT Secret is not configured"

**Problem**: The Admin API doesn't have JWT secrets configured.

**Solution**: Run the JWT secrets setup script:
```powershell
# Windows
.\scripts\setup-jwt-secrets-all.ps1

# Linux/macOS
./scripts/setup-jwt-secrets-all.sh
```

### Error: "Unauthorized" when using token from Main API on Admin API

**Problem**: The two APIs are using different JWT secrets.

**Solution**: Ensure both APIs use the SAME secret. Re-run the setup script or manually verify:
```bash
# Check Main API secret
cd src/Archu.Api
dotnet user-secrets list

# Check Admin API secret
cd ../../Archu.AdminApi
dotnet user-secrets list

# They should be identical
```

## 📖 Full Documentation

For complete Admin API documentation, including:
- All available endpoints
- Security best practices
- Troubleshooting guide
- Production deployment tips

**See**: [Archu.AdminApi/README.md](../Archu.AdminApi/README.md)

---

**Last Updated**: 2025-01-22  
**Maintainer**: Archu Development Team
