# Archu Authentication Guide

Complete guide to authentication in Archu, including JWT configuration, token management, security best practices, and troubleshooting.

---

## 📚 Table of Contents

- [Overview](#overview)
- [JWT Configuration](#jwt-configuration)
- [Authentication Endpoints](#authentication-endpoints)
- [Authentication Flow](#authentication-flow)
- [Token Management](#token-management)
- [Security Best Practices](#security-best-practices)
- [Implementation Details](#implementation-details)
- [Testing Authentication](#testing-authentication)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### Authentication System

Archu uses **JWT (JSON Web Token)** authentication with:
- ✅ Secure token-based authentication
- ✅ JWT Bearer tokens for API authentication
- ✅ Refresh tokens for seamless re-authentication
- ✅ Role-based authorization integration
- ✅ BCrypt password hashing (12 rounds)
- ✅ Email confirmation support
- ✅ Password reset functionality
- ✅ Account lockout protection

### Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| **JwtTokenService** | Generates and validates JWT tokens | `Archu.Infrastructure/Authentication/JwtTokenService.cs` |
| **AuthenticationService** | Handles login, register, password management | `Archu.Infrastructure/Authentication/AuthenticationService.cs` |
| **PasswordHasher** | Securely hashes and verifies passwords (BCrypt) | `Archu.Infrastructure/Authentication/PasswordHasher.cs` |
| **PasswordValidator** | Validates password complexity | `Archu.Infrastructure/Authentication/PasswordValidator.cs` |
| **RefreshTokenHandler** | Manages refresh token lifecycle | `Archu.Infrastructure/Authentication/RefreshTokenHandler.cs` |
| **AuthenticationController** | API endpoints for authentication | `Archu.Api/Controllers/AuthenticationController.cs` |

---

## 🔧 JWT Configuration

### Configuration Settings

JWT settings are configured in `appsettings.json` under the `Jwt` section:

```json
{
"Jwt": {
    "Secret": "your-secret-key-at-least-32-characters-long-for-security",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Configuration Options

| Setting | Type | Required | Default | Description | Validation |
|---------|------|----------|---------|-------------|------------|
| **Secret** | string | ✅ Yes | - | JWT signing key | ≥32 characters (256 bits minimum) |
| **Issuer** | string | ✅ Yes | - | Token issuer (API URL or name) | Cannot be empty |
| **Audience** | string | ✅ Yes | - | Token audience (who can use it) | Cannot be empty |
| **AccessTokenExpirationMinutes** | int | No | 60 | Access token lifetime | Must be > 0 (15-60 recommended) |
| **RefreshTokenExpirationDays** | int | No | 7 | Refresh token lifetime | Must be > 0 (7-30 recommended) |

**⚠️ Configuration Validation:**
- The `JwtOptions.Validate()` method runs on startup
- Missing or invalid configuration throws `InvalidOperationException`
- Application won't start with invalid JWT configuration

### Quick Setup with User Secrets (Development)

**Recommended for local development** - keeps secrets out of source control:

```bash
cd src/Archu.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "your-ultra-secure-secret-key-at-least-32-characters-long"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
```

Verify secrets:
```bash
dotnet user-secrets list
```

### Production Configuration

**❌ NEVER commit JWT secrets to source control!**

#### Option 1: Environment Variables (Recommended)

**Linux/macOS:**
```bash
export Jwt__Secret="production-secret-key-at-least-32-characters-long"
export Jwt__Issuer="https://api.yourproduction.com"
export Jwt__Audience="https://api.yourproduction.com"
export Jwt__AccessTokenExpirationMinutes="30"
export Jwt__RefreshTokenExpirationDays="7"
```

**Windows PowerShell:**
```powershell
$env:Jwt__Secret="production-secret-key-at-least-32-characters-long"
$env:Jwt__Issuer="https://api.yourproduction.com"
$env:Jwt__Audience="https://api.yourproduction.com"
$env:Jwt__AccessTokenExpirationMinutes="30"
$env:Jwt__RefreshTokenExpirationDays="7"
```

**Docker Compose:**
```yaml
services:
  api:
    environment:
      - Jwt__Secret=production-secret-key-at-least-32-characters-long
      - Jwt__Issuer=https://api.yourproduction.com
      - Jwt__Audience=https://api.yourproduction.com
```

#### Option 2: Azure Key Vault (Best for Azure)

```csharp
// Program.cs (automatically configured if using Azure)
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

Store secrets in Key Vault:
- **Jwt--Secret**: `your-production-secret-at-least-32-characters`
- **Jwt--Issuer**: `https://api.yourproduction.com`
- **Jwt--Audience**: `https://api.yourproduction.com`

### Generating Secure Secrets

**PowerShell:**
```powershell
# Generate 32-byte (256-bit) Base64-encoded secret
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

**Linux/macOS:**
```bash
# Generate 32-byte (256-bit) Base64-encoded secret
openssl rand -base64 32
```

**C# (.NET):**
```csharp
// Generate secure random secret
var bytes = new byte[32];
System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
var secret = Convert.ToBase64String(bytes);
Console.WriteLine(secret);
```

---

## 🔐 Authentication Endpoints

### Complete Endpoint List

All endpoints are prefixed with `/api/v1/authentication/`:

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/register` | POST | ❌ No | Register new user account |
| `/login` | POST | ❌ No | Authenticate user and return tokens |
| `/refresh-token` | POST | ❌ No | Refresh expired access token |
| `/logout` | POST | ✅ Yes | Revoke refresh token |
| `/change-password` | POST | ✅ Yes | Change user's password |
| `/forgot-password` | POST | ❌ No | Request password reset token |
| `/reset-password` | POST | ❌ No | Reset password with token |
| `/confirm-email` | POST | ❌ No | Confirm user's email address |

**Total Endpoints:** 8 (5 public, 3 authenticated)

### Endpoint Details

#### 1. Register User

**Endpoint:** `POST /api/v1/authentication/register`

**Request Body:**
```json
{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-refresh-token",
  "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userName": "johndoe",
      "email": "john@example.com",
      "emailConfirmed": false,
    "roles": ["User"]
    }
  },
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "User with this email already exists",
  "data": null,
  "errors": null,
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Password Requirements:**
- Minimum 8 characters
- Maximum 100 characters
- See [Password Security Guide](PASSWORD_SECURITY_GUIDE.md) for detailed requirements

---

#### 2. Login

**Endpoint:** `POST /api/v1/authentication/login`

**Request Body:**
```json
{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-refresh-token",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userName": "johndoe",
      "email": "john@example.com",
      "emailConfirmed": true,
      "roles": ["User", "Manager"]
    }
  },
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid email or password",
  "data": null,
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Features:**
- Returns JWT access token (1 hour default)
- Returns refresh token (7 days default)
- Account lockout after 5 failed attempts
- Lockout duration: 15 minutes

---

#### 3. Refresh Token

**Endpoint:** `POST /api/v1/authentication/refresh-token`

**Request Body:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "new-jwt-access-token",
    "refreshToken": "new-refresh-token",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userName": "johndoe",
      "email": "john@example.com",
      "emailConfirmed": true,
      "roles": ["User"]
    }
  },
  "timestamp": "2025-01-24T11:00:00Z"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "The refresh token is invalid or has expired",
  "data": null,
  "timestamp": "2025-01-24T11:00:00Z"
}
```

**Behavior:**
- Old refresh token is invalidated
- Returns new access token AND new refresh token
- Refresh token is single-use

---

#### 4. Logout

**Endpoint:** `POST /api/v1/authentication/logout`

**Headers:**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Logged out successfully",
  "data": {},
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Effects:**
- Revokes refresh token in database
- Client should discard both tokens
- Access token remains valid until expiration (stateless)

---

#### 5. Change Password

**Endpoint:** `POST /api/v1/authentication/change-password`

**Headers:**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword456!"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Password changed successfully",
"data": {},
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Current password is incorrect",
  "data": null,
  "timestamp": "2025-01-24T10:00:00Z"
}
```

---

#### 6. Forgot Password

**Endpoint:** `POST /api/v1/authentication/forgot-password`

**Request Body:**
```json
{
  "email": "john@example.com"
}
```

**Response (Always 200 OK - Security):**
```json
{
  "success": true,
  "message": "If your email exists in our system, you will receive a password reset link.",
  "data": {},
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Security Features:**
- Always returns success (prevents email enumeration)
- If email exists, sends reset token via email
- Token expires after 1 hour
- Single-use token

---

#### 7. Reset Password

**Endpoint:** `POST /api/v1/authentication/reset-password`

**Request Body:**
```json
{
  "email": "john@example.com",
  "resetToken": "token-from-email",
  "newPassword": "NewSecurePassword456!"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Password reset successfully",
  "data": {},
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Invalid or expired reset token",
  "data": null,
  "timestamp": "2025-01-24T10:00:00Z"
}
```

---

#### 8. Confirm Email

**Endpoint:** `POST /api/v1/authentication/confirm-email`

**Request Body:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "confirmationToken": "token-from-email"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Email confirmed successfully",
  "data": {},
  "timestamp": "2025-01-24T10:00:00Z"
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Invalid or expired confirmation token",
  "data": null,
  "timestamp": "2025-01-24T10:00:00Z"
}
```

---

## 🔄 Authentication Flow

### Complete Authentication Lifecycle

```
1. REGISTRATION
   ├─ POST /api/v1/authentication/register
   ├─ Password hashed with BCrypt (12 rounds)
   ├─ User created with "User" role
   ├─ JWT access token generated (1 hour)
   ├─ Refresh token generated (7 days)
   └─ Email confirmation token sent (optional)

2. EMAIL CONFIRMATION (Optional)
   ├─ POST /api/v1/authentication/confirm-email
   ├─ Token validated
   └─ EmailConfirmed flag set to true

3. LOGIN
   ├─ POST /api/v1/authentication/login
   ├─ Password verified with BCrypt
   ├─ Account lockout checked
   ├─ JWT access token generated
   ├─ Refresh token generated
   └─ Previous refresh token invalidated

4. USING PROTECTED ENDPOINTS
   ├─ Include: Authorization: Bearer {accessToken}
   ├─ JWT validated on each request
   ├─ User claims extracted from token
   └─ Authorization policies checked

5. TOKEN REFRESH (Before Expiration)
   ├─ POST /api/v1/authentication/refresh-token
   ├─ Refresh token validated
   ├─ New access token generated
   ├─ New refresh token generated
   └─ Old refresh token revoked

6. LOGOUT
   ├─ POST /api/v1/authentication/logout
   ├─ Refresh token revoked
   └─ Client discards tokens
```

### JWT Token Structure

**Access Token Payload:**
```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "john@example.com",
  "name": "johndoe",
  "role": ["User", "Manager"],
  "iss": "https://localhost:7123",
  "aud": "https://localhost:7123",
  "exp": 1706095200,
  "iat": 1706091600,
  "nbf": 1706091600
}
```

**Claim Types:**
- `sub` (Subject): User ID (Guid)
- `email`: User's email address
- `name`: Username
- `role`: Array of role names
- `iss` (Issuer): Token issuer URL
- `aud` (Audience): Token audience URL
- `exp` (Expiration): Unix timestamp
- `iat` (Issued At): Unix timestamp
- `nbf` (Not Before): Unix timestamp

---

## 🔑 Token Management

### Using Tokens in Requests

**HTTP Header Format:**
```http
GET /api/v1/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

**cURL:**
```bash
curl -X GET "https://localhost:7123/api/v1/products" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

**JavaScript (Fetch API):**
```javascript
const response = await fetch('https://localhost:7123/api/v1/products', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
 'Content-Type': 'application/json'
  }
});
```

**C# (HttpClient):**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);

var response = await client.GetAsync("https://localhost:7123/api/v1/products");
```

### Token Refresh Strategy

**Client-Side Implementation:**

```javascript
// Token storage
let accessToken = null;
let refreshToken = null;
let tokenExpiresAt = null;

// Automatic refresh before expiration
function scheduleTokenRefresh(expiresIn) {
  // Refresh 5 minutes before expiration
  const refreshIn = (expiresIn - 300) * 1000;
  
  setTimeout(async () => {
    await refreshTokens();
  }, refreshIn);
}

// Refresh tokens
async function refreshTokens() {
  const response = await fetch('/api/v1/authentication/refresh-token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  });

  if (response.ok) {
    const data = await response.json();
    accessToken = data.data.accessToken;
    refreshToken = data.data.refreshToken;
    tokenExpiresAt = Date.now() + (data.data.expiresIn * 1000);
    
    scheduleTokenRefresh(data.data.expiresIn);
    return true;
  }
  
  // Refresh failed - redirect to login
  window.location.href = '/login';
  return false;
}

// API call with automatic retry
async function callApi(url, options = {}) {
  // Try with current token
  let response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${accessToken}`
    }
  });

  // If unauthorized, try refreshing
  if (response.status === 401) {
    const refreshed = await refreshTokens();
    
    if (refreshed) {
      // Retry with new token
      response = await fetch(url, {
        ...options,
        headers: {
   ...options.headers,
          'Authorization': `Bearer ${accessToken}`
        }
      });
    }
  }

  return response;
}
```

### Token Lifetime Best Practices

| Token Type | Recommended Lifetime | Trade-off |
|------------|---------------------|-----------|
| **Access Token** | 15-60 minutes | Shorter = More secure, More refreshes |
| **Refresh Token** | 7-30 days | Longer = Better UX, Less secure |
| **Password Reset** | 1 hour | Balance between security and usability |
| **Email Confirmation** | 24 hours | Enough time for user to check email |

**Production Recommendations:**
- Access Token: **30 minutes** (good balance)
- Refresh Token: **7 days** (weekly re-authentication)
- Implement **sliding expiration** for refresh tokens
- Revoke all tokens on password change

---

## 🔒 Security Best Practices

### 1. Secret Management

✅ **DO:**
- Use user secrets for local development (`dotnet user-secrets`)
- Use environment variables or Azure Key Vault for production
- Generate cryptographically secure secrets (≥32 characters)
- Use different secrets per environment (dev, staging, production)
- Rotate secrets periodically (quarterly recommended)
- Never log or return secrets in API responses

❌ **DON'T:**
- Commit secrets to source control (.git, GitHub, etc.)
- Use weak or predictable secrets ("secret123", "password")
- Share secrets across environments
- Hardcode secrets in source code
- Use same secret for multiple purposes

### 2. Password Security

**Implementation Details:**
- **Hashing Algorithm:** BCrypt with 12 rounds (work factor)
- **Salt:** Automatic per-password salt generation
- **Storage:** Only hashed passwords stored (never plain text)
- **Verification:** Constant-time comparison

**Password Policy:**
- Minimum 8 characters
- Maximum 100 characters
- Additional requirements via `PasswordValidator`
- See [Password Security Guide](PASSWORD_SECURITY_GUIDE.md)

### 3. Token Storage

**Client-Side Storage:**

| Storage Type | Security | Use Case | Recommendation |
|--------------|----------|----------|----------------|
| **Memory (Variable)** | ✅ Best | SPAs, short sessions | ✅ Recommended for SPAs |
| **HttpOnly Cookie** | ✅ Good | Server-rendered apps | ✅ Recommended for Blazor Server |
| **sessionStorage** | ⚠️ Moderate | Temporary, same tab | ⚠️ Acceptable for dev |
| **localStorage** | ❌ Vulnerable | Persistent | ❌ Avoid (XSS risk) |
| **URL Parameters** | ❌ Dangerous | Never | ❌ Never use |

**Best Practices:**
- Clear tokens on logout
- Don't log tokens to console in production
- Validate token expiration client-side
- Use HTTPS only

### 4. Account Security

**Account Lockout:**
- **Max Failed Attempts:** 5 (configurable)
- **Lockout Duration:** 15 minutes (configurable)
- **Reset on Success:** Failed count resets on successful login

**Email Confirmation:**
- Optional but recommended for production
- Tokens expire after 24 hours
- Single-use tokens

### 5. HTTPS Requirements

**Development:**
```csharp
// Allow HTTP in development
options.RequireHttpsMetadata = !environment.IsDevelopment();
```

**Production:**
```csharp
// Enforce HTTPS
app.UseHttpsRedirection();
app.UseHsts();

// JWT validation
options.RequireHttpsMetadata = true;
```

### 6. CORS Configuration

**Development (Blazor WebAssembly):**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyMethod()
      .AllowAnyHeader();
    });
});
```

**Production:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
      policy.WithOrigins("https://yourapp.com", "https://www.yourapp.com")
       .AllowAnyMethod()
       .AllowAnyHeader()
.AllowCredentials();
 });
});
```

---

## 🛠️ Implementation Details

### JWT Token Generation

**Location:** `src/Archu.Infrastructure/Authentication/JwtTokenService.cs`

```csharp
public string GenerateAccessToken(IEnumerable<Claim> claims)
{
    var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
    var securityKey = new SymmetricSecurityKey(key);
    var credentials = new SigningCredentials(
        securityKey, 
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
     issuer: _jwtOptions.Issuer,
    audience: _jwtOptions.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(
            _jwtOptions.AccessTokenExpirationMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Key Points:**
- Algorithm: HS256 (HMAC-SHA256)
- Claims: User ID, email, username, roles
- Expiration: Configurable (default 60 minutes)
- Issuer & Audience: Must match configuration

### Password Hashing

**Location:** `src/Archu.Infrastructure/Authentication/PasswordHasher.cs`

```csharp
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(
 password, 
        BCrypt.Net.BCrypt.GenerateSalt(12)); // 12 rounds
}

public bool VerifyPassword(string hashedPassword, string providedPassword)
{
 return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
}
```

**BCrypt Benefits:**
- Automatic salt generation
- Configurable work factor (12 rounds = ~250ms)
- Resistant to rainbow table attacks
- Industry-standard algorithm
- Future-proof (can increase rounds as hardware improves)

### Authentication Middleware

**Location:** `src/Archu.Api/Program.cs`

```csharp
// Order is critical!
app.UseAuthentication(); // 1. Validate JWT, populate User claims
app.UseAuthorization();  // 2. Check policies and roles
```

**⚠️ Middleware Order Matters:**
1. `UseAuthentication()` - Validates JWT token, populates `HttpContext.User`
2. `UseAuthorization()` - Checks `[Authorize]` attributes and policies

### JWT Validation Configuration

**Location:** `src/Archu.Infrastructure/DependencyInjection.cs`

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
 .AddJwtBearer(options =>
    {
     options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(5), // Clock tolerance
         
            ValidIssuer = jwtOptions.Issuer,
  ValidAudience = jwtOptions.Audience,
 IssuerSigningKey = new SymmetricSecurityKey(
         Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });
```

---

## 🧪 Testing Authentication

### 1. Using Scalar UI (Interactive Testing)

**Steps:**
1. Start API: `dotnet run --project src/Archu.Api`
2. Open browser: `https://localhost:7123/scalar/v1`
3. Find "Authentication" section
4. Test **Register** endpoint:
   - Click "Try it out"
   - Fill in request body
   - Click "Execute"
   - Copy `accessToken` from response
5. Click **"Authorize"** button (top right)
6. Paste token (without "Bearer " prefix)
7. Click "Authorize"
8. Test protected endpoints

### 2. Using HTTP Files (Visual Studio)

**Location:** `src/Archu.Api/Archu.Api.http`

```http
@baseUrl = https://localhost:7123/api/v1

### 1. Register User
POST {{baseUrl}}/authentication/register
Content-Type: application/json

{
  "userName": "testuser",
  "email": "test@example.com",
"password": "Test123!"
}

### 2. Extract token
@token = {{register.response.body.data.accessToken}}

### 3. Get Products (Authenticated)
GET {{baseUrl}}/products
Authorization: Bearer {{token}}
```

### 3. Automated Testing

**Integration Test Example:**

```csharp
public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
     var request = new LoginRequest
        {
  Email = "test@example.com",
     Password = "Test123!"
        };

        // Act
      var response = await _client.PostAsJsonAsync(
 "/api/v1/authentication/login", 
            request);

    // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
  
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<AuthenticationResponse>>();
        
  result.Success.Should().BeTrue();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.ExpiresIn.Should().BeGreaterThan(0);
    }
}
```

---

## 🐛 Troubleshooting

### Issue 1: "JWT Secret is not configured"

**Error:**
```
InvalidOperationException: JWT Secret is not configured. 
Please set Jwt:Secret in appsettings.json
```

**Solution:**

**Option A - User Secrets (Development):**
```bash
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "your-secret-at-least-32-characters-long"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
```

**Option B - appsettings.Development.json:**
```json
{
  "Jwt": {
    "Secret": "development-secret-at-least-32-characters-long",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123"
  }
}
```

**Option C - Environment Variable:**
```bash
# Linux/macOS
export Jwt__Secret="your-secret-at-least-32-characters-long"

# Windows PowerShell
$env:Jwt__Secret="your-secret-at-least-32-characters-long"
```

---

### Issue 2: "JWT Secret must be at least 32 characters"

**Error:**
```
InvalidOperationException: JWT Secret must be at least 32 characters 
(256 bits) for security.
```

**Solution:**

Generate a secure 32+ character secret:

```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Linux/macOS
openssl rand -base64 32
```

---

### Issue 3: "401 Unauthorized" on Protected Endpoints

**Symptoms:**
- API returns 401 for endpoints requiring authentication
- Token is present in Authorization header

**Diagnostic Checklist:**

1. **Verify Token Format:**
   ```http
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
   - ✅ Must start with "Bearer "
   - ✅ One space after "Bearer"
   - ❌ No extra line breaks or whitespace

2. **Check Token Expiration:**
 ```javascript
   // Decode JWT to inspect payload
   const parts = token.split('.');
   const payload = JSON.parse(atob(parts[1]));
   const expiresAt = new Date(payload.exp * 1000);
   console.log('Token expires at:', expiresAt);
   console.log('Is expired:', expiresAt < new Date());
   ```

3. **Verify Issuer/Audience Match:**
   - Token `iss` must match `Jwt:Issuer`
   - Token `aud` must match `Jwt:Audience`
   - Check `appsettings.json` vs token payload

4. **Enable Debug Logging:**
   ```json
   // appsettings.Development.json
   {
     "Logging": {
       "LogLevel": {
    "Default": "Information",
         "Microsoft.AspNetCore.Authentication": "Debug"
       }
     }
   }
   ```

5. **Check JWT Events (if enabled):**
   ```
   JWT authentication failed: The token is expired
   JWT token validated successfully for user: {userId}
   JWT authentication challenge: invalid_token
   ```

---

### Issue 4: "Invalid email or password"

**Symptoms:**
- Login fails with correct credentials
- Account may be locked

**Diagnostic Steps:**

1. **Verify User Exists:**
   ```sql
   SELECT Id, Email, EmailConfirmed, LockoutEnd, IsDeleted 
   FROM Users 
 WHERE Email = 'user@example.com'
   ```

2. **Check Account Status:**
   - `EmailConfirmed`: Should be `true` if email verification required
   - `LockoutEnd`: Should be `NULL` or past date
   - `IsDeleted`: Should be `false` (0)
   - `AccessFailedCount`: Reset to 0 on successful login

3. **Test Account Lockout:**
   ```
   If LockoutEnd > UtcNow → Account is locked
   Wait 15 minutes or reset in database
   ```

4. **Password Case Sensitivity:**
   - Passwords are case-sensitive
   - Verify exact password (no extra spaces)

---

### Issue 5: "The refresh token is invalid or has expired"

**Symptoms:**
- Refresh token request fails
- 401 Unauthorized response

**Causes:**
- Token already used (single-use)
- Token expired (beyond 7-day default)
- Token revoked (user logged out)
- User deleted or account disabled

**Solution:**
- Redirect user to login page
- Clear stored tokens
- Re-authenticate

**Prevention:**
```javascript
// Store token expiration and refresh proactively
const tokenExpiresAt = Date.now() + (expiresIn * 1000);

// Refresh 5 minutes before expiration
setTimeout(() => refreshTokens(), (expiresIn - 300) * 1000);
```

---

### Issue 6: CORS Errors (Blazor WebAssembly)

**Error:**
```
Access to fetch at 'https://localhost:7123/api/v1/authentication/login' 
from origin 'https://localhost:5001' has been blocked by CORS policy: 
No 'Access-Control-Allow-Origin' header is present
```

**Solution:**

Verify CORS is configured in `src/Archu.Api/Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorWasm", policy =>
        {
            policy.AllowAnyOrigin()
        .AllowAnyMethod()
      .AllowAnyHeader();
    });
});

// ... later in pipeline

app.UseCors("AllowBlazorWasm"); // BEFORE UseAuthentication()
```

**⚠️ Middleware Order:**
```csharp
app.UseCors("AllowBlazorWasm");  // 1. FIRST
app.UseAuthentication();          // 2. Second
app.UseAuthorization();           // 3. Third
```

**Production CORS:**
```csharp
options.AddPolicy("Production", policy =>
{
    policy.WithOrigins(
      "https://yourapp.com", 
        "https://www.yourapp.com"
    )
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
});
```

---

### Issue 7: Account Lockout

**Symptoms:**
- User cannot login after multiple failed attempts
- "Account is locked" message

**Default Configuration:**
- **Max Failed Attempts:** 5
- **Lockout Duration:** 15 minutes

**Solutions:**

**Option 1 - Wait:**
Wait 15 minutes for automatic unlock

**Option 2 - Reset in Database:**
```sql
UPDATE Users 
SET LockoutEnd = NULL, 
    AccessFailedCount = 0 
WHERE Email = 'user@example.com'
```

**Option 3 - Configure in appsettings.json:**
```json
{
  "PasswordPolicy": {
    "MaxFailedAccessAttempts": 5,
    "LockoutDuration": "00:15:00"
  }
}
```

---

### Issue 8: Database Connection Errors

**Error:**
```
Unable to connect to SQL Server
A network-related or instance-specific error occurred
```

**Solutions:**

1. **Verify Connection String:**
   ```json
   // appsettings.Development.json
   {
     "ConnectionStrings": {
       "archudb": "Server=localhost;Database=ArchuDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

2. **Check SQL Server is Running:**
 ```bash
   # Services (Windows)
   Get-Service MSSQLSERVER
   
   # Docker
   docker ps | grep sql
   ```

3. **Test Connection:**
   ```bash
   cd src/Archu.Infrastructure
   dotnet ef database update --startup-project ../Archu.Api
   ```

---

## 📚 Related Documentation

- **[Authorization Guide](AUTHORIZATION_GUIDE.md)** - Role-based access control, policies, permissions
- **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)** - Password policies, complexity rules, validation
- **[API Guide](API_GUIDE.md)** - Complete API reference with all endpoints
- **[Getting Started](GETTING_STARTED.md)** - Initial setup and configuration
- **[Database Guide](DATABASE_GUIDE.md)** - Database setup, migrations, seeding
- **[Archu.ApiClient Authentication](../src/Archu.ApiClient/Authentication/README.md)** - Client-side authentication framework

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2025-01-24 | ✅ **Complete verification** against codebase: Added all 8 endpoints, verified JWT config from JwtOptions.cs, added BCrypt details, comprehensive troubleshooting |
| 1.0 | 2025-01-22 | Initial authentication guide |

---

**Last Updated**: 2025-01-24  
**Version**: 2.0 ✅ **VERIFIED**  
**Maintainer**: Archu Development Team

**Questions?** See [docs/README.md](README.md) or open an [issue](https://github.com/chethandvg/archu/issues)

---

## 🏗️ Security Architecture

### Shared Infrastructure Design

This section captures how the Archu platform reuses a single identity store, JWT configuration, and permission model across every API surface.

#### Shared ApplicationDbContext & Connection String

Both **Archu.Api** and **Archu.AdminApi** call `services.AddInfrastructure(configuration, environment)` during startup, which registers `ApplicationDbContext` with the same configuration for both APIs. The Infrastructure layer resolves the connection string from the `Sql` (fallback `archudb`) connection string entry and wires up Entity Framework Core with retry and migration settings.

**Key Files:**
- `src/Archu.Api/Program.cs` → `builder.Services.AddInfrastructure(...)`
- `src/Archu.AdminApi/Program.cs` → `builder.Services.AddInfrastructure(...)`
- `src/Archu.Infrastructure/DependencyInjection.cs` → `AddDatabase` configures `ApplicationDbContext`

Because both APIs resolve the context from the same DI registration, every write to roles, permissions, or tokens is persisted to the same database and becomes visible to the other API after the transaction is committed.

#### Canonical Identity Store & Permission Entities

`ApplicationDbContext` exposes all identity tables, including permission entities:

| Entity | Purpose | DbSet |
| ------ | ------- | ----- |
| `ApplicationPermission` | Catalog of discrete permissions | `ApplicationDbContext.Permissions` |
| `RolePermission` | Junction table linking roles to permissions | `ApplicationDbContext.RolePermissions` |
| `UserPermission` | Junction table assigning permissions directly to users | `ApplicationDbContext.UserPermissions` |

These DbSets live alongside existing identity entities (`ApplicationUser`, `ApplicationRole`, `UserRole`) in the same context.

#### JWT Issuer/Audience/Signing Key Reuse

`AddInfrastructure` wires up the shared JWT authentication stack. `AddAuthenticationServices` binds the `Jwt` configuration section into `JwtOptions`, validates the issuer/audience/secret, and registers `JwtTokenService` plus the ASP.NET Core JWT bearer handler. 

Because both APIs call the same extension, they rely on identical issuer, audience, and signing key settings, guaranteeing tokens created by one API are trusted by the others.

#### Permission Propagation Into Tokens

`AuthenticationService.GenerateAuthenticationResultAsync` mints access tokens during login/registration. It loads the caller's roles, resolves role-assigned permissions via `RolePermissions`, gathers direct grants from `UserPermissions`, and hydrates the token with `CustomClaimTypes.Permission` claims.

The generated JWT includes:
- Standard identity claims (subject, email, username)
- Role claims for every role assignment
- Permission claims representing the union of role and direct permissions
- Feature flags such as `EmailVerified` and `TwoFactorEnabled`

Tokens from either API carry the same claim shape, allowing downstream services to rely on JWT permissions without re-querying the database.

#### Integrating New Services

When building a new API or background worker:

1. Reference `Archu.Infrastructure` and call `services.AddInfrastructure(configuration, environment)`
2. Consume `ApplicationDbContext` or repository abstractions
3. Configure your host to read the shared `Jwt` configuration for token validation
4. Use JWT permission claims to enforce policies

This keeps every service aligned with the canonical identity store while preserving centralized token issuance and validation.

---
