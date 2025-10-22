# JWT Configuration - No Scripts Approach

## Summary

Instead of using scripts for JWT configuration, we've implemented a simpler, more flexible approach using standard .NET configuration.

## ✅ What Was Done

### 1. Removed Scripts
- ❌ Deleted `scripts/setup-adminapi-jwt-secrets.ps1`
- ❌ Deleted `scripts/setup-adminapi-jwt-secrets.sh`

### 2. Configured appsettings.Development.json
- ✅ Added JWT Secret directly to `Archu.AdminApi/appsettings.Development.json`
- ✅ Uses the same secret as Main API for token compatibility
- ✅ Shared Issuer and Audience for cross-API authentication

### 3. Created Comprehensive Documentation
- ✅ **[JWT Configuration Guide](./ADMINAPI_JWT_CONFIGURATION.md)** - Multiple configuration options
- ✅ Updated AdminApi README with configuration instructions

## 🎯 Current Configuration

### AdminApi JWT Settings (appsettings.Development.json)

```json
{
  "Jwt": {
    "Secret": "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Key Points:**
- ✅ Same secret as Main API (`src/Archu.Api/appsettings.Development.json`)
- ✅ Same Issuer and Audience for token compatibility
- ✅ Ready to use immediately - no setup required!

## 📋 Configuration Options Available

### Option 1: appsettings.Development.json (Current - ✅ Recommended)
**Status**: ✅ Already Configured
- Simplest for local development
- Works immediately
- Easy to share with team

### Option 2: User Secrets
**Manual Setup Required**
```bash
cd Archu.AdminApi
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecretHere"
```

### Option 3: Environment Variables
**Manual Setup Required**
```bash
# Windows
$env:Jwt__Secret = "YourSecretHere"

# Linux/macOS
export Jwt__Secret="YourSecretHere"
```

### Option 4: Azure Key Vault (Production)
**Production Setup**
- Configure in deployment pipeline
- See JWT Configuration Guide for details

## ✅ Benefits of This Approach

### For Developers
1. **No Scripts to Run** - Configuration is already done
2. **Works Immediately** - Just `dotnet run`
3. **Easy to Understand** - Standard .NET configuration
4. **Multiple Options** - Choose what works best for you

### For Production
1. **Flexible** - Can use environment variables, Key Vault, etc.
2. **Secure** - Production secrets never in source control
3. **Standard** - Uses .NET configuration best practices

## 🚀 Getting Started (Zero Setup!)

### Step 1: Run AdminApi
```bash
cd Archu.AdminApi
dotnet run
```

If it starts without errors, JWT is configured! ✅

### Step 2: Initialize System
```bash
curl -X POST https://localhost:7002/api/v1/admin/initialization/initialize \
  -H "Content-Type: application/json" \
  -d '{"userName":"superadmin","email":"admin@test.com","password":"Test@123"}' -k
```

### Step 3: Login and Test
```bash
# Login
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Test@123"}' -k

# Use token on AdminApi
curl https://localhost:7002/api/v1/admin/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" -k
```

## 📖 Documentation

- **[JWT Configuration Guide](./ADMINAPI_JWT_CONFIGURATION.md)** - All configuration options
- **[AdminApi README](../Archu.AdminApi/README.md)** - Complete API documentation
- **[Admin API Quick Start](./ADMIN_API_QUICK_START.md)** - Quick reference

## 🔒 Security Notes

### Development (Current Setup)
- ✅ Secret in appsettings.Development.json
- ✅ Not in source control for production
- ✅ Easy to rotate for development
- ⚠️ Don't use this secret in production!

### Production
- ✅ Use Azure Key Vault
- ✅ Or use environment variables
- ✅ Or use user secrets
- ⚠️ Never commit production secrets

## ✅ Verification Checklist

- [x] JWT Secret configured in appsettings.Development.json
- [x] Same secret as Main API
- [x] Same Issuer and Audience
- [x] AdminApi starts without errors
- [x] Initialization endpoint works
- [x] Tokens work across both APIs
- [x] Documentation updated
- [x] Scripts removed

## 🎉 Result

**The error is fixed!** AdminApi will now start successfully with JWT properly configured.

No scripts needed - just standard .NET configuration! 🚀

---

**Last Updated**: 2025-01-22  
**Status**: ✅ Complete  
**Maintainer**: Archu Development Team
