# Quick Start: JWT Authentication

## 🚀 TL;DR - Get Started in 5 Minutes

### 1. Run the API
```bash
cd src/Archu.Api
dotnet run
```

### 2. Register a User
```bash
curl -X POST https://localhost:7001/api/v1/authentication/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "userName": "testuser"
  }'
```

### 3. Login
```bash
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

**Copy the `accessToken` from the response!**

### 4. Use Protected Endpoints
```bash
curl -X GET https://localhost:7001/api/v1/products \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

## 📋 Available Endpoints

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `POST /api/v1/authentication/register` | ❌ | Create new account |
| `POST /api/v1/authentication/login` | ❌ | Get access token |
| `POST /api/v1/authentication/refresh-token` | ❌ | Refresh expired token |
| `POST /api/v1/authentication/logout` | ✅ | Invalidate tokens |
| `POST /api/v1/authentication/change-password` | ✅ | Change password |
| `POST /api/v1/authentication/forgot-password` | ❌ | Request reset token |
| `POST /api/v1/authentication/reset-password` | ❌ | Reset with token |
| `POST /api/v1/authentication/confirm-email` | ❌ | Verify email |

## 🔐 Protecting Your Endpoints

### Basic Authentication
```csharp
[HttpGet]
[Authorize] // Requires valid JWT token
public async Task<IActionResult> GetSecureData()
{
    return Ok("This is protected data");
}
```

### Get Current User
```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetMyProfile()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = User.FindFirst(ClaimTypes.Email)?.Value;
    
    return Ok(new { userId, email });
}
```

### Role-Based Access
```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")] // Only admins can delete
public async Task<IActionResult> DeleteResource(Guid id)
{
    // Delete logic
}
```

## 🛠️ Configuration

### appsettings.Development.json
```json
{
  "Jwt": {
    "Secret": "Your-Secret-Key-At-Least-32-Characters-Long!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

## 📊 Token Lifetime

| Token Type | Default Lifetime | Purpose |
|------------|-----------------|---------|
| **Access Token** | 60 minutes | API authorization |
| **Refresh Token** | 7 days | Renew access tokens |

## 🔍 Testing with Scalar UI

1. Navigate to: https://localhost:7001/scalar/v1
2. Find the "Authorize" button (top right)
3. Enter: `Bearer {your-access-token}`
4. Click "Authorize"
5. All protected endpoints now work!

## ⚠️ Common Issues

### "401 Unauthorized"
- Check if token is expired (60 min default)
- Use refresh token to get a new access token
- Verify token is in header: `Authorization: Bearer {token}`

### "Secret must be at least 32 characters"
- Update `Jwt:Secret` in appsettings.json
- Must be **exactly 32 characters or more**

### "Invalid token"
- Token may be expired
- Issuer/Audience mismatch
- Check clock sync between client and server

## 📝 Example: Complete Authentication Flow

```csharp
// 1. Register
var registerRequest = new { 
    email = "user@example.com", 
    password = "SecurePass123!", 
    userName = "john" 
};
var registerResponse = await httpClient.PostAsJsonAsync(
    "/api/v1/authentication/register", 
    registerRequest);

// 2. Extract tokens
var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
var accessToken = authResult.Data.AccessToken;
var refreshToken = authResult.Data.RefreshToken;

// 3. Use access token
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);

// 4. Make authenticated requests
var products = await httpClient.GetFromJsonAsync<List<Product>>("/api/v1/products");

// 5. Refresh when access token expires
var refreshRequest = new { refreshToken };
var refreshResponse = await httpClient.PostAsJsonAsync(
    "/api/v1/authentication/refresh-token", 
    refreshRequest);
var newTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
```

## 🎯 Next Steps

- [ ] Implement email confirmation
- [ ] Add role management
- [ ] Configure production secrets (Azure Key Vault)
- [ ] Add rate limiting
- [ ] Implement 2FA (optional)

## 📚 Full Documentation

See [JWT_IMPLEMENTATION.md](./JWT_IMPLEMENTATION.md) for complete details.

---

**Need Help?** Check the logs in the API output for detailed authentication errors.
