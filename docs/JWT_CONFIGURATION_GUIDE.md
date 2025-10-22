# JWT Configuration Guide

This guide explains how to configure JWT authentication for the Archu application, including secure secret storage using .NET User Secrets and Azure Key Vault.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Configuration Options](#configuration-options)
3. [Secure Secret Storage](#secure-secret-storage)
4. [Environment-Specific Configuration](#environment-specific-configuration)
5. [Production Deployment](#production-deployment)
6. [Troubleshooting](#troubleshooting)

---

## Quick Start

### 1. Basic Configuration (appsettings.json)

The JWT configuration is already set up in `appsettings.json` and `appsettings.Development.json`:

```json
{
  "Jwt": {
    "Secret": "REPLACE_THIS_WITH_SECURE_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG_FOR_PRODUCTION",
    "Issuer": "https://api.archu.com",
    "Audience": "https://api.archu.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 2. Registration in Program.cs

The Infrastructure layer provides a clean `AddInfrastructure` extension method that registers all services including JWT:

```csharp
// In Program.cs
using Archu.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add all Infrastructure services (Database, Authentication, Repositories)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ... rest of configuration
```

This replaces the manual registration and keeps your Program.cs clean.

---

## Configuration Options

### JwtOptions Properties

| Property | Type | Description | Default | Recommended |
|----------|------|-------------|---------|-------------|
| `Secret` | `string` | Signing key (min 32 chars) | - | **Required** |
| `Issuer` | `string` | Token issuer (your API) | - | `https://api.yourapp.com` |
| `Audience` | `string` | Token audience | - | `https://api.yourapp.com` |
| `AccessTokenExpirationMinutes` | `int` | Access token lifetime | 60 | 15-60 minutes |
| `RefreshTokenExpirationDays` | `int` | Refresh token lifetime | 7 | 7-30 days |

### Security Requirements

✅ **Secret Requirements:**
- Minimum 32 characters (256 bits)
- Use cryptographically random characters
- Never commit to source control
- Rotate periodically in production

✅ **Issuer & Audience:**
- Use HTTPS URLs for production
- Can be the same value for single-app scenarios
- Should match your actual API URL

---

## Secure Secret Storage

### ⚠️ Never Store Secrets in Code

**DON'T DO THIS:**
```json
{
  "Jwt": {
    "Secret": "MyWeakPassword123!"  // ❌ NEVER commit secrets to Git
  }
}
```

### Option 1: .NET User Secrets (Local Development)

User Secrets is perfect for local development and keeps secrets out of source control.

#### Step 1: Initialize User Secrets

From the **API project directory**:

```bash
cd src/Archu.Api
dotnet user-secrets init
```

This adds a `<UserSecretsId>` to your `.csproj` file.

#### Step 2: Set JWT Secret

```bash
dotnet user-secrets set "Jwt:Secret" "YourSecure32CharacterOrLongerSecretKeyForDevelopment!@#$%"
```

You can also set other JWT properties:

```bash
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7001"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7001"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

#### Step 3: List Your Secrets

```bash
dotnet user-secrets list
```

#### Step 4: Remove User Secrets (if needed)

```bash
dotnet user-secrets remove "Jwt:Secret"
# or clear all secrets
dotnet user-secrets clear
```

#### Where Are User Secrets Stored?

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
```

**macOS/Linux:**
```
~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

### Option 2: Environment Variables

Environment variables work across all environments (dev, staging, production).

#### Windows (PowerShell)

```powershell
$env:Jwt__Secret = "YourSecure32CharacterOrLongerSecretKeyForProduction!@#$%"
$env:Jwt__Issuer = "https://api.archu.com"
$env:Jwt__Audience = "https://api.archu.com"
```

#### Linux/macOS (Bash)

```bash
export Jwt__Secret="YourSecure32CharacterOrLongerSecretKeyForProduction!@#$%"
export Jwt__Issuer="https://api.archu.com"
export Jwt__Audience="https://api.archu.com"
```

#### Docker

```dockerfile
# docker-compose.yml
environment:
  - Jwt__Secret=YourSecure32CharacterOrLongerSecretKeyForProduction!@#$%
  - Jwt__Issuer=https://api.archu.com
  - Jwt__Audience=https://api.archu.com
```

#### Azure App Service

```bash
az webapp config appsettings set --name myapp --resource-group mygroup \
  --settings Jwt__Secret="YourSecure32CharacterOrLongerSecretKeyForProduction!@#$%"
```

### Option 3: Azure Key Vault (Production)

Azure Key Vault is the recommended approach for production environments.

#### Step 1: Install NuGet Package

```bash
cd src/Archu.Api
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

#### Step 2: Update Program.cs

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"]
        ?? throw new InvalidOperationException("KeyVault URL not configured");

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Add Infrastructure services (includes JWT authentication)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ... rest of configuration
```

#### Step 3: Store Secrets in Key Vault

```bash
# Create Key Vault
az keyvault create --name archu-keyvault --resource-group archu-rg --location eastus

# Add JWT secret
az keyvault secret set --vault-name archu-keyvault --name "Jwt--Secret" \
  --value "YourSecure32CharacterOrLongerSecretKeyForProduction!@#$%"

# Add other JWT settings
az keyvault secret set --vault-name archu-keyvault --name "Jwt--Issuer" \
  --value "https://api.archu.com"

az keyvault secret set --vault-name archu-keyvault --name "Jwt--Audience" \
  --value "https://api.archu.com"
```

**Note:** Azure Key Vault uses `--` instead of `:` for nested configuration keys.

#### Step 4: Grant Access to App Service

```bash
# Enable managed identity for App Service
az webapp identity assign --name myapp --resource-group mygroup

# Grant Key Vault access
az keyvault set-policy --name archu-keyvault \
  --object-id <app-service-principal-id> \
  --secret-permissions get list
```

---

## Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "Jwt": {
    "Secret": "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Or use User Secrets (recommended):**

```bash
dotnet user-secrets set "Jwt:Secret" "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!"
```

### Staging (appsettings.Staging.json)

```json
{
  "Jwt": {
    "Issuer": "https://staging-api.archu.com",
    "Audience": "https://staging-api.archu.com",
    "AccessTokenExpirationMinutes": 30
  }
}
```

**Secret stored in Azure Key Vault or environment variables.**

### Production (appsettings.Production.json)

```json
{
  "Jwt": {
    "Issuer": "https://api.archu.com",
    "Audience": "https://api.archu.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "KeyVault": {
    "Url": "https://archu-keyvault.vault.azure.net/"
  }
}
```

**Secret stored in Azure Key Vault.**

---

## Production Deployment

### Generating a Secure Secret

Use this C# code to generate a secure 256-bit (32 character) secret:

```csharp
using System.Security.Cryptography;

var randomBytes = new byte[32]; // 256 bits
using var rng = RandomNumberGenerator.Create();
rng.GetBytes(randomBytes);
var secret = Convert.ToBase64String(randomBytes);
Console.WriteLine($"Generated JWT Secret: {secret}");
```

Or use PowerShell:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

### Deployment Checklist

- ✅ Generate a cryptographically secure secret (min 32 characters)
- ✅ Store secret in Azure Key Vault or secure environment variables
- ✅ Use HTTPS URLs for Issuer and Audience
- ✅ Set short access token expiration (15-30 minutes)
- ✅ Enable `RequireHttpsMetadata` in production
- ✅ Configure proper CORS policies
- ✅ Enable logging for authentication failures
- ✅ Set up secret rotation policy
- ✅ Monitor token usage and expiration

### Azure App Service Configuration

```bash
# Set environment variables
az webapp config appsettings set --name archu-api --resource-group archu-rg \
  --settings \
    Jwt__Secret="<secure-secret-from-keyvault>" \
    Jwt__Issuer="https://api.archu.com" \
    Jwt__Audience="https://api.archu.com" \
    Jwt__AccessTokenExpirationMinutes="15" \
    Jwt__RefreshTokenExpirationDays="7" \
    ASPNETCORE_ENVIRONMENT="Production"
```

---

## Troubleshooting

### Error: "JWT Secret is not configured"

**Solution:** Set the `Jwt:Secret` in User Secrets, environment variables, or Azure Key Vault.

```bash
dotnet user-secrets set "Jwt:Secret" "YourSecure32CharacterOrLongerSecretKey!"
```

### Error: "JWT Secret must be at least 32 characters"

**Solution:** Generate a longer secret. The secret must be at least 256 bits (32 characters).

```bash
# Generate a secure 256-bit secret
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 32)"
```

### Error: "JWT Issuer is not configured"

**Solution:** Set the `Jwt:Issuer` in configuration.

```bash
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7001"
```

### Token Validation Fails

**Check:**
1. Token expiration (`exp` claim)
2. Issuer matches (`iss` claim)
3. Audience matches (`aud` claim)
4. Signing key is correct
5. Clock skew settings (default: 5 minutes)

**Enable detailed logging:**

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  }
}
```

### Tokens Expire Too Quickly

**Solution:** Adjust expiration times in configuration.

For development:
```bash
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "120"
```

For production (keep short for security):
```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

## Configuration Priority

.NET configuration sources are evaluated in this order (last wins):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments
6. Azure Key Vault (if configured)

**Example:** User Secrets will override `appsettings.Development.json`, and Azure Key Vault will override everything.

---

## Security Best Practices

1. ✅ **Never commit secrets to Git**
   - Use `.gitignore` to exclude `appsettings.Development.json` if it contains secrets
   - Use User Secrets for local development

2. ✅ **Use strong secrets**
   - Minimum 32 characters (256 bits)
   - Use cryptographically random characters
   - Generate using `RandomNumberGenerator`

3. ✅ **Rotate secrets periodically**
   - Implement a secret rotation policy (e.g., every 90 days)
   - Use Azure Key Vault's automatic rotation features

4. ✅ **Use short token lifetimes**
   - Access tokens: 15-30 minutes
   - Refresh tokens: 7-30 days
   - Implement token refresh mechanism

5. ✅ **Monitor authentication failures**
   - Enable structured logging
   - Set up alerts for suspicious activity
   - Track failed authentication attempts

6. ✅ **Use HTTPS in production**
   - Enable `RequireHttpsMetadata`
   - Use HTTPS URLs for Issuer and Audience
   - Implement HSTS headers

---

## Example: Complete Setup

### 1. Development Setup

```bash
# Initialize User Secrets
cd src/Archu.Api
dotnet user-secrets init

# Set JWT configuration
dotnet user-secrets set "Jwt:Secret" "ArchuDevSecretKey32CharactersLong!@#"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7001"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7001"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

### 2. Update Program.cs

```csharp
using Archu.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (includes JWT authentication)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ... rest of configuration

var app = builder.Build();

app.UseAuthentication(); // Enable JWT authentication
app.UseAuthorization();  // Enable authorization

app.Run();
```

### 3. Verify Configuration

Run the application and check the logs:

```
info: Archu.Infrastructure.DependencyInjection[0]
      JWT authentication configured successfully
      Issuer: https://localhost:7001
      Audience: https://localhost:7001
      Access Token Expiration: 60 minutes
```

---

## Related Documentation

- 📖 [Architecture Guide](ARCHITECTURE.md)
- 📖 [Authentication Service Documentation](../src/Archu.Infrastructure/Authentication/README.md)
- 📖 [API Authentication Controller](../src/Archu.Api/Controllers/AuthenticationController.cs)
- 📖 [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- 📖 [Azure Key Vault Configuration](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
