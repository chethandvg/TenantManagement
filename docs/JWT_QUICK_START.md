# JWT Authentication - Quick Start Guide

Get JWT authentication up and running in 5 minutes.

---

## 🚀 Quick Setup (3 Steps)

### Step 1: Run the Setup Script

**Windows (PowerShell):**
```powershell
cd src/Archu.Api
../../scripts/setup-jwt-secrets.ps1
```

**Linux/macOS:**
```bash
cd src/Archu.Api
chmod +x ../../scripts/setup-jwt-secrets.sh
../../scripts/setup-jwt-secrets.sh
```

This script will:
- Initialize User Secrets
- Generate a secure 256-bit JWT secret
- Configure JWT settings (Issuer, Audience, Expiration)

### Step 2: Verify Configuration

```bash
dotnet user-secrets list
```

You should see:
```
Jwt:Secret = <your-secure-secret>
Jwt:Issuer = https://localhost:7001
Jwt:Audience = https://localhost:7001
Jwt:AccessTokenExpirationMinutes = 60
Jwt:RefreshTokenExpirationDays = 7
```

### Step 3: Run the Application

```bash
dotnet run
```

✅ **Done!** JWT authentication is now configured and ready to use.

---

## 🧪 Test Authentication

### 1. Open API Documentation

Navigate to: https://localhost:7001/scalar/v1

### 2. Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@archu.com",
  "password": "Admin@123"
}
```

### 3. Copy Access Token

From the response, copy the `accessToken` value.

### 4. Authenticate Requests

Click "Authorize" in Scalar UI and paste your token:
```
Bearer <your-access-token>
```

### 5. Access Protected Endpoints

Try any protected endpoint (e.g., GET /api/v1/products).

---

## 📋 Manual Setup (Alternative)

If you prefer to set up manually without the script:

### 1. Initialize User Secrets

```bash
cd src/Archu.Api
dotnet user-secrets init
```

### 2. Generate a Secure Secret

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

### 3. Set JWT Configuration

```bash
dotnet user-secrets set "Jwt:Secret" "<your-generated-secret>"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7001"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7001"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

---

## 🔧 Configuration Options

### Development (User Secrets)
✅ Secure (not in Git)  
✅ Easy to change  
✅ Developer-specific  

**Best for:** Local development

### Staging/Production (Azure Key Vault)
✅ Enterprise-grade security  
✅ Automatic rotation  
✅ Audit logging  

**Best for:** Production deployments

See [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md) for Azure Key Vault setup.

---

## 🛡️ Security Checklist

- ✅ JWT Secret is at least 32 characters
- ✅ Secret is NOT committed to Git
- ✅ Using User Secrets for local development
- ✅ HTTPS enabled in production
- ✅ Short access token lifetime (15-60 minutes)
- ✅ Refresh tokens enabled for seamless re-authentication

---

## 🐛 Troubleshooting

### "JWT Secret is not configured"

**Fix:**
```bash
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "YourSecure32CharacterOrLongerSecretKey!"
```

### "JWT Secret must be at least 32 characters"

**Fix:** Generate a longer secret using the commands above.

### Token validation fails

**Check:**
1. Token hasn't expired (`exp` claim)
2. Issuer matches configuration (`iss` claim)
3. Audience matches configuration (`aud` claim)

---

## 📚 Next Steps

1. ✅ Read the [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)
2. ✅ Explore the [Authentication Infrastructure README](../src/Archu.Infrastructure/Authentication/README.md)
3. ✅ Set up database seeding (next task)
4. ✅ Implement password policies (after seeding)
5. ✅ Add user-specific data protection (final task)

---

## 💡 Useful Commands

```bash
# View all secrets
dotnet user-secrets list

# Update a specific secret
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "30"

# Remove a secret
dotnet user-secrets remove "Jwt:Secret"

# Clear all secrets
dotnet user-secrets clear
```

---

**Last Updated**: 2025-01-22  
**Maintainer**: Archu Development Team
