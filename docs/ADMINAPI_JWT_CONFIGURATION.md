# JWT Configuration Guide for AdminApi

## Overview

The AdminApi requires JWT configuration to authenticate users. This guide provides multiple options for configuring JWT secrets without using scripts.

## ⚠️ Important: Token Compatibility

For tokens to work across both `Archu.Api` and `Archu.AdminApi`, both APIs must have:
- ✅ **Same JWT Secret**
- ✅ **Same Issuer**
- ✅ **Same Audience**

This allows a token issued by the Main API to be used on Admin API endpoints and vice versa.

## 🎯 Configuration Options

### Option 1: appsettings.Development.json (✅ Recommended for Local Development)

**Pros:**
- ✅ Simplest setup
- ✅ No additional tools needed
- ✅ Works immediately
- ✅ Easy to share with team (via Git)

**Cons:**
- ⚠️ Secret is in source control (but only development secret)
- ⚠️ Not suitable for production

**Setup:**

The AdminApi is already configured with a JWT secret in `appsettings.Development.json`:

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

**✅ This matches the Main API configuration** - No additional setup needed!

**To change the secret (optional):**
1. Open `Archu.AdminApi/appsettings.Development.json`
2. Change the `Jwt:Secret` value (must be at least 32 characters)
3. Make sure to update `src/Archu.Api/appsettings.Development.json` with the same secret

---

### Option 2: User Secrets (Recommended for Production-Like Development)

**Pros:**
- ✅ Secrets not in source control
- ✅ Per-developer configuration
- ✅ Production-like security
- ✅ Easy to manage with dotnet CLI

**Cons:**
- ⚠️ Each developer must set up individually
- ⚠️ Requires manual setup

**Setup:**

#### Step 1: Initialize User Secrets

```bash
# For Main API
cd src/Archu.Api
dotnet user-secrets init

# For Admin API
cd ../../Archu.AdminApi
dotnet user-secrets init
```

#### Step 2: Set JWT Secret (Same for Both APIs)

```bash
# Set secret for Main API
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"

# Set secret for Admin API
cd ../../Archu.AdminApi
dotnet user-secrets set "Jwt:Secret" "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"
```

**Important:** Use the **exact same secret** for both APIs!

#### Step 3: Verify Secrets

```bash
# View Main API secrets
cd src/Archu.Api
dotnet user-secrets list

# View Admin API secrets
cd ../../Archu.AdminApi
dotnet user-secrets list
```

Both should show the same JWT secret.

#### Step 4: Update appsettings.Development.json

Remove the `Jwt:Secret` line from `appsettings.Development.json` (keep other Jwt settings):

```json
{
  "Jwt": {
    // "Secret": "...",  // Remove this line - will come from user secrets
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

### Option 3: Environment Variables

**Pros:**
- ✅ Secrets not in source control
- ✅ Works in containers and cloud
- ✅ Production-ready approach

**Cons:**
- ⚠️ Must be set for each terminal session
- ⚠️ Can be tedious for local development

**Setup:**

#### Windows (PowerShell)

```powershell
# Set for current session
$env:Jwt__Secret = "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"

# Or set permanently for user
[System.Environment]::SetEnvironmentVariable("Jwt__Secret", "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!", "User")
```

#### Linux/macOS

```bash
# Set for current session
export Jwt__Secret="ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"

# Or add to ~/.bashrc or ~/.zshrc for persistence
echo 'export Jwt__Secret="ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"' >> ~/.bashrc
source ~/.bashrc
```

**Note:** Double underscores (`__`) in environment variables represent nested configuration (equivalent to `Jwt:Secret` in JSON).

---

### Option 4: launchSettings.json (Per-Developer Configuration)

**Pros:**
- ✅ Not in source control (.gitignore includes launchSettings.json)
- ✅ Easy to set up
- ✅ Works in Visual Studio and dotnet CLI

**Cons:**
- ⚠️ Only works when launching from the project directory
- ⚠️ Doesn't work with Aspire AppHost

**Setup:**

Create or edit `Archu.AdminApi/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "Archu.AdminApi": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:7002;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "Jwt__Secret": "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"
      }
    }
  }
}
```

---

## 🔐 Production Configuration

### Option A: Azure Key Vault (Recommended for Azure)

**Setup:**

1. Create Azure Key Vault
2. Add secret named `Jwt--Secret`
3. Configure app to use Key Vault:

```csharp
// In Program.cs
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{keyVaultName}.vault.azure.net/"),
        new DefaultAzureCredential());
}
```

### Option B: AWS Secrets Manager (For AWS)

Use AWS Secrets Manager SDK to retrieve secrets at runtime.

### Option C: Kubernetes Secrets

Store secrets in Kubernetes and mount as environment variables or files.

---

## 📋 Quick Setup Guide (Current Configuration)

**✅ For Immediate Use (Already Done):**

Your AdminApi is already configured with JWT secret in `appsettings.Development.json` that matches the Main API. You can start using it immediately!

```bash
cd Archu.AdminApi
dotnet run
```

**✅ The error is now fixed!** The JWT secret is configured.

---

## 🧪 Verify Configuration

### Test 1: Check Configuration is Loaded

```bash
cd Archu.AdminApi
dotnet run
```

If it starts without JWT errors, configuration is correct!

### Test 2: Initialize System

```bash
curl -X POST https://localhost:7002/api/v1/admin/initialization/initialize \
  -H "Content-Type: application/json" \
  -d '{"userName":"superadmin","email":"admin@test.com","password":"Test@123"}' -k
```

### Test 3: Login and Get Token

```bash
# Login via Main API
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Test@123"}' -k
```

### Test 4: Use Token on Admin API

```bash
# Use the token from step 3
curl https://localhost:7002/api/v1/admin/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" -k
```

If all tests pass, JWT is configured correctly!

---

## 🔄 Configuration Priority

.NET loads configuration in this order (later overrides earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments

**For your setup:**
- Development: Uses `appsettings.Development.json` (already configured)
- Production: Should use Environment Variables or Key Vault

---

## 🚨 Troubleshooting

### Error: "JWT Secret is not configured"

**Solution:** Check configuration priority. Secret might be overridden by empty value in a later configuration source.

**Debug:**
```csharp
// Add to Program.cs temporarily
var secret = builder.Configuration["Jwt:Secret"];
Console.WriteLine($"JWT Secret loaded: {secret?.Substring(0, 10)}...");
```

### Error: Tokens from Main API don't work on Admin API

**Check:**
1. Both APIs have the same `Jwt:Secret`
2. Both APIs have the same `Jwt:Issuer`
3. Both APIs have the same `Jwt:Audience`
4. Token hasn't expired

### Error: "The configured user secrets ID is invalid"

**Solution:** Re-initialize user secrets:
```bash
dotnet user-secrets init --force
```

---

## 📝 Best Practices

### Development
- ✅ Use `appsettings.Development.json` for simplicity
- ✅ Share same secret between Main and Admin API
- ✅ Use a long, complex secret (32+ characters)
- ⚠️ Never commit production secrets

### Production
- ✅ Use Azure Key Vault, AWS Secrets Manager, or similar
- ✅ Rotate secrets regularly
- ✅ Use different secrets for different environments
- ✅ Monitor secret access logs

---

## 📚 Additional Resources

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [User Secrets in Development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)

---

**Current Status**: ✅ **AdminApi is configured and ready to use!**

The JWT secret is already set in `appsettings.Development.json` and matches the Main API configuration.

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
