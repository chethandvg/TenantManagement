# Security Configuration Guide

## Overview

This guide covers all security-related configuration options in the Archu application, including password policies, JWT authentication, and security best practices.

---

## 📋 Table of Contents

1. [Password Policy Configuration](#password-policy-configuration)
2. [JWT Authentication Configuration](#jwt-authentication-configuration)
3. [Environment-Specific Settings](#environment-specific-settings)
4. [Security Best Practices](#security-best-practices)
5. [Configuration Validation](#configuration-validation)

---

## 🔐 Password Policy Configuration

### Location
`appsettings.json` or `appsettings.{Environment}.json`

### Complete Configuration

```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 0,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  }
}
```

### Configuration Options

| Setting | Type | Default | Min/Max | Description |
|---------|------|---------|---------|-------------|
| `MinimumLength` | int | 8 | 4-256 | Minimum password length |
| `MaximumLength` | int | 128 | MinLen-256 | Maximum password length |
| `RequireUppercase` | bool | true | - | Require A-Z characters |
| `RequireLowercase` | bool | true | - | Require a-z characters |
| `RequireDigit` | bool | true | - | Require 0-9 characters |
| `RequireSpecialCharacter` | bool | true | - | Require special characters |
| `MinimumUniqueCharacters` | int | 0 | 0-MinLen | Minimum distinct characters |
| `PreventCommonPasswords` | bool | true | - | Block 100+ common passwords |
| `PreventUserInfo` | bool | true | - | Block username/email in password |
| `SpecialCharacters` | string | "!@#$..." | - | Valid special characters |

### Configuration Presets

#### Standard Security (Recommended)
```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 0,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  }
}
```

#### High Security (Financial/Healthcare)
```json
{
  "PasswordPolicy": {
    "MinimumLength": 12,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 8,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  }
}
```

#### Relaxed Security (Internal Apps)
```json
{
  "PasswordPolicy": {
    "MinimumLength": 6,
    "MaximumLength": 128,
    "RequireUppercase": false,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": false,
    "MinimumUniqueCharacters": 0,
    "PreventCommonPasswords": true,
    "PreventUserInfo": false,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  }
}
```

#### Development/Testing
```json
{
  "PasswordPolicy": {
    "MinimumLength": 4,
    "MaximumLength": 128,
    "RequireUppercase": false,
    "RequireLowercase": false,
    "RequireDigit": false,
    "RequireSpecialCharacter": false,
    "MinimumUniqueCharacters": 0,
    "PreventCommonPasswords": false,
    "PreventUserInfo": false,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  }
}
```

⚠️ **Warning**: Development settings should NEVER be used in production!

### Validation Rules

The configuration is validated on application startup. Invalid configurations will cause the application to fail with a descriptive error:

```csharp
// Validation rules enforced:
MinimumLength >= 4
MaximumLength >= MinimumLength
MaximumLength <= 256
MinimumUniqueCharacters >= 0
MinimumUniqueCharacters <= MinimumLength
```

---

## 🎫 JWT Authentication Configuration

### Location
`appsettings.json` or environment variables (recommended for production)

### Complete Configuration

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "https://yourdomain.com",
    "Audience": "https://yourdomain.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Configuration Options

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Secret` | string | (required) | Signing key (min 32 chars) |
| `Issuer` | string | (required) | Token issuer URL |
| `Audience` | string | (required) | Token audience URL |
| `AccessTokenExpirationMinutes` | int | 60 | Access token lifetime |
| `RefreshTokenExpirationDays` | int | 7 | Refresh token lifetime |

### Environment-Specific JWT Configuration

#### Development
```json
{
  "Jwt": {
    "Secret": "development-secret-key-minimum-32-characters-for-testing-only",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "Jwt": {
    "Secret": "USE_ENVIRONMENT_VARIABLE_FOR_THIS",
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://yourdomain.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Storing JWT Secret Securely

#### ❌ Never Do This (Hardcoded in appsettings.json)
```json
{
  "Jwt": {
    "Secret": "my-production-secret-key-123456789"
  }
}
```

#### ✅ Use Environment Variables (Production)

**Linux/macOS:**
```bash
export Jwt__Secret="your-production-secret-key-at-least-32-chars"
export Jwt__Issuer="https://api.yourdomain.com"
export Jwt__Audience="https://yourdomain.com"
```

**Windows PowerShell:**
```powershell
$env:Jwt__Secret="your-production-secret-key-at-least-32-chars"
$env:Jwt__Issuer="https://api.yourdomain.com"
$env:Jwt__Audience="https://yourdomain.com"
```

**Azure App Service (Application Settings):**
```
Jwt__Secret = <stored in Azure Key Vault>
Jwt__Issuer = https://api.yourdomain.com
Jwt__Audience = https://yourdomain.com
```

**Docker Compose:**
```yaml
environment:
  - Jwt__Secret=${JWT_SECRET}
  - Jwt__Issuer=https://api.yourdomain.com
  - Jwt__Audience=https://yourdomain.com
```

#### ✅ Use Azure Key Vault (Recommended)

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Then reference secrets:
```json
{
  "Jwt": {
    "Secret": "@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/JwtSecret/)",
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://yourdomain.com"
  }
}
```

---

## 🌍 Environment-Specific Settings

### File Structure
```
appsettings.json                    // Base settings
appsettings.Development.json        // Development overrides
appsettings.Staging.json            // Staging overrides
appsettings.Production.json         // Production overrides
```

### Development Settings

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Archu": "Debug"
    }
  },
  "PasswordPolicy": {
    "MinimumLength": 6,
    "RequireSpecialCharacter": false
  },
  "Jwt": {
    "Secret": "development-secret-key-minimum-32-characters",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123",
    "AccessTokenExpirationMinutes": 120
  },
  "ConnectionStrings": {
    "Sql": "Server=localhost;Database=ArchuDev;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Staging Settings

**appsettings.Staging.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "PasswordPolicy": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true
  },
  "Jwt": {
    "Issuer": "https://staging-api.yourdomain.com",
    "Audience": "https://staging.yourdomain.com",
    "AccessTokenExpirationMinutes": 30
  }
}
```

### Production Settings

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Archu": "Information"
    }
  },
  "PasswordPolicy": {
    "MinimumLength": 12,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 8,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true
  },
  "Jwt": {
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://yourdomain.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

## 🔒 Security Best Practices

### 1. JWT Secret Management

✅ **Do:**
- Use cryptographically secure random keys (minimum 32 characters)
- Store secrets in environment variables or Azure Key Vault
- Rotate JWT secrets periodically (e.g., every 90 days)
- Use different secrets for each environment
- Never commit secrets to source control

❌ **Don't:**
- Hardcode secrets in appsettings.json
- Use weak or predictable secrets
- Share production secrets with developers
- Store secrets in plain text files
- Use the same secret across environments

### 2. Password Policy Configuration

✅ **Do:**
- Enable all complexity requirements in production
- Set minimum length to at least 8 characters (12+ recommended)
- Enable common password prevention
- Enable user info prevention
- Configure strong special character requirements
- Use higher security for privileged accounts

❌ **Don't:**
- Weaken password requirements in production
- Allow passwords shorter than 8 characters
- Disable common password checks
- Allow username in passwords
- Use development settings in production

### 3. Token Expiration

✅ **Do:**
- Use short-lived access tokens (15-60 minutes)
- Use longer-lived refresh tokens (7-30 days)
- Implement token revocation
- Store refresh tokens securely (encrypted in database)
- Rotate refresh tokens on use

❌ **Don't:**
- Use long-lived access tokens (hours/days)
- Store tokens in browser local storage (XSS risk)
- Allow unlimited refresh token usage
- Forget to implement token cleanup
- Use refresh tokens without rotation

### 4. HTTPS Configuration

✅ **Do:**
- Enforce HTTPS in production
- Use TLS 1.2 or higher
- Implement HSTS (HTTP Strict Transport Security)
- Use strong cipher suites
- Validate SSL certificates

❌ **Don't:**
- Allow HTTP in production
- Use self-signed certificates in production
- Disable certificate validation
- Use weak SSL/TLS versions

---

## ✔️ Configuration Validation

### Startup Validation

The application validates all security configurations on startup and fails fast with descriptive errors:

```csharp
// Validated on startup:
✅ Password policy options (Validate() method)
✅ JWT options (Validate() method)
✅ Database connection string
✅ Required configuration sections
```

### Manual Validation

You can manually validate configuration using the testing tools:

```bash
# Test password policy configuration
dotnet run --project src/Archu.Api -- --validate-config

# Test JWT configuration
dotnet run --project src/Archu.Api -- --test-jwt
```

### Common Validation Errors

#### Password Policy

```
❌ InvalidOperationException: Minimum password length must be at least 4 characters.
Fix: Set PasswordPolicy:MinimumLength >= 4

❌ InvalidOperationException: Maximum password length must be greater than or equal to minimum length.
Fix: Ensure PasswordPolicy:MaximumLength >= PasswordPolicy:MinimumLength

❌ InvalidOperationException: Minimum unique characters cannot exceed minimum length.
Fix: Set PasswordPolicy:MinimumUniqueCharacters <= PasswordPolicy:MinimumLength
```

#### JWT Configuration

```
❌ InvalidOperationException: JWT Secret must be at least 32 characters long.
Fix: Use a longer secret key (32+ characters)

❌ InvalidOperationException: JWT Issuer is required.
Fix: Set Jwt:Issuer in configuration

❌ InvalidOperationException: JWT Audience is required.
Fix: Set Jwt:Audience in configuration
```

---

## 📊 Configuration Templates

### Minimum Required Configuration

```json
{
  "ConnectionStrings": {
    "Sql": "Server=localhost;Database=Archu;Trusted_Connection=True;"
  },
  "Jwt": {
    "Secret": "minimum-32-character-secret-key-required-here",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123"
  },
  "PasswordPolicy": {
    "MinimumLength": 8
  }
}
```

### Complete Production Configuration

```json
{
  "ConnectionStrings": {
    "Sql": "Server=prod-server;Database=Archu;User Id=sa;Password=***;TrustServerCertificate=False;"
  },
  "Jwt": {
    "Secret": "USE_AZURE_KEY_VAULT_OR_ENV_VARIABLE",
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://yourdomain.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "PasswordPolicy": {
    "MinimumLength": 12,
    "MaximumLength": 128,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "MinimumUniqueCharacters": 8,
    "PreventCommonPasswords": true,
    "PreventUserInfo": true,
    "SpecialCharacters": "!@#$%^&*()_+-=[]{}|;:,.<>?~`"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Archu": "Information"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "yourdomain.com,*.yourdomain.com"
}
```

---

## 🔧 Troubleshooting

### Issue: Configuration Not Loading

**Symptoms:**
- Default values being used
- Configuration validation errors
- Null reference exceptions

**Solutions:**
1. Check file naming: `appsettings.{Environment}.json`
2. Verify `ASPNETCORE_ENVIRONMENT` environment variable
3. Ensure JSON is valid (no trailing commas)
4. Check file is set to "Copy to Output Directory"

### Issue: Environment Variables Not Working

**Symptoms:**
- Environment variables ignored
- Configuration using appsettings values instead

**Solutions:**
1. Use correct delimiter: `__` (double underscore)
   ```bash
   # ✅ Correct
   Jwt__Secret=mysecret
   
   # ❌ Incorrect
   Jwt:Secret=mysecret
   ```

2. Restart application after setting variables
3. Check variable scope (user vs system)
4. Verify order: Environment variables override appsettings

### Issue: Validation Errors on Startup

**Symptoms:**
- Application fails to start
- Configuration validation exception

**Solutions:**
1. Read the validation error message carefully
2. Check configuration values against constraints
3. Validate JSON syntax
4. Ensure all required settings are present

---

## 📚 Related Documentation

- **[Password Complexity Validation](PASSWORD_COMPLEXITY_VALIDATION.md)** - Detailed password validation guide
- **[Authentication Framework](../src/Archu.ApiClient/Authentication/README.md)** - JWT authentication
- **[Architecture Guide](ARCHITECTURE.md)** - Clean Architecture principles
- **[API Documentation](../src/Archu.Api/README.md)** - API endpoints

---

## ✅ Security Checklist

Use this checklist before deploying to production:

### Password Security
- [ ] Minimum length set to 8+ characters (12+ recommended)
- [ ] All character type requirements enabled
- [ ] Common password prevention enabled
- [ ] User info prevention enabled
- [ ] Configuration validated on startup

### JWT Security
- [ ] Secret key is 32+ characters
- [ ] Secret stored in environment variable or Key Vault
- [ ] Access token expiration ≤ 60 minutes
- [ ] Refresh token expiration ≤ 30 days
- [ ] HTTPS enforced (`RequireHttpsMetadata = true`)
- [ ] Different secrets for each environment

### Database Security
- [ ] Connection string uses strong password
- [ ] TrustServerCertificate disabled in production
- [ ] Connection string stored securely
- [ ] Database access restricted by IP/firewall
- [ ] Regular backups configured

### General Security
- [ ] HTTPS enforced
- [ ] HSTS enabled
- [ ] CORS properly configured
- [ ] Rate limiting implemented
- [ ] Logging configured (without sensitive data)
- [ ] Security headers configured

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
