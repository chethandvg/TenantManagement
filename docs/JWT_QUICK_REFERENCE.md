# JWT Token Generation - Quick Reference

## 🚀 Quick Start

### Configuration (appsettings.json)

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyAtLeast32CharactersLong!",
    "Issuer": "https://api.archu.com",
    "Audience": "https://api.archu.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Dependency Injection (Program.cs)

```csharp
// Configure JWT Options
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// Register Services
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<RefreshTokenHandler>();

// Configure Authentication
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
jwtOptions.Validate();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// In middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
```

---

## 📝 Common Usage Patterns

### 1. Generate Access Token

```csharp
public class AuthenticationService
{
    private readonly IJwtTokenService _jwtTokenService;
    
    public string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        return _jwtTokenService.GenerateAccessToken(
            userId: user.Id.ToString(),
            email: user.Email,
            userName: user.UserName,
            roles: roles);
    }
}
```

### 2. Generate and Store Refresh Token

```csharp
public class AuthenticationService
{
    private readonly RefreshTokenHandler _refreshTokenHandler;
    
    public (string token, DateTime expires) CreateRefreshToken(ApplicationUser user)
    {
        return _refreshTokenHandler.GenerateAndStoreRefreshToken(user);
    }
}
```

### 3. Validate Refresh Token

```csharp
public async Task<bool> IsRefreshTokenValid(ApplicationUser user, string refreshToken)
{
    return _refreshTokenHandler.ValidateRefreshToken(user, refreshToken);
}
```

### 4. Rotate Refresh Token

```csharp
public (string newToken, DateTime expires) RefreshAccessToken(ApplicationUser user)
{
    // This generates a new refresh token and invalidates the old one
    return _refreshTokenHandler.RotateRefreshToken(user);
}
```

### 5. Revoke Refresh Token (Logout)

```csharp
public void Logout(ApplicationUser user)
{
    _refreshTokenHandler.RevokeRefreshToken(user);
}
```

### 6. Validate Access Token

```csharp
public ClaimsPrincipal? ValidateAccessToken(string token)
{
    return _jwtTokenService.ValidateToken(token);
}
```

---

## 🔐 Controller Examples

### Protect Endpoint (Requires Authentication)

```csharp
[Authorize]
[HttpGet]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
{
    // Only accessible with valid access token
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = User.FindFirstValue(ClaimTypes.Email);
    
    // ... implementation
}
```

### Role-Based Authorization

```csharp
[Authorize(Roles = ApplicationRoles.Admin)]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteProduct(Guid id)
{
    // Only accessible by Admin users
}
```

### Manual Token Validation in Controller

```csharp
[HttpGet("validate")]
public ActionResult ValidateToken()
{
    var authHeader = Request.Headers["Authorization"].ToString();
    var token = authHeader.Replace("Bearer ", "");
    
    var principal = _jwtTokenService.ValidateToken(token);
    
    if (principal is null)
        return Unauthorized("Invalid or expired token");
    
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    
    return Ok(new { userId, roles });
}
```

---

## 🔑 Claims Available in Token

```csharp
// Standard JWT claims
var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
var userName = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

// .NET Framework claims (backward compatibility)
var userId2 = User.FindFirstValue(ClaimTypes.NameIdentifier);
var email2 = User.FindFirstValue(ClaimTypes.Email);
var name = User.FindFirstValue(ClaimTypes.Name);

// Roles
var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
var isAdmin = User.IsInRole(ApplicationRoles.Admin);
```

---

## 📊 Token Expiration Helpers

```csharp
// Get expiration times
var accessTokenLifetime = _jwtTokenService.GetAccessTokenExpiration(); // TimeSpan
var refreshTokenLifetime = _jwtTokenService.GetRefreshTokenExpiration(); // TimeSpan

// Get expiry dates
var accessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiryUtc(); // DateTime
var refreshTokenExpiresAt = _jwtTokenService.GetRefreshTokenExpiryUtc(); // DateTime
```

---

## 🧪 Testing

### Generate Test Token

```csharp
var jwtOptions = Options.Create(new JwtOptions
{
    Secret = "TestSecretKeyAtLeast32CharactersLong!",
    Issuer = "test-issuer",
    Audience = "test-audience",
    AccessTokenExpirationMinutes = 60,
    RefreshTokenExpirationDays = 7
});

var tokenService = new JwtTokenService(jwtOptions);

var token = tokenService.GenerateAccessToken(
    userId: "test-user-id",
    email: "test@example.com",
    userName: "testuser",
    roles: new[] { "User", "Admin" });
```

### Add Token to HTTP Client

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("https://api.example.com/products");
```

---

## ⚠️ Security Notes

### DO ✅
- Store JWT secret in Azure Key Vault or environment variables
- Use HTTPS for all API endpoints
- Set short expiration for access tokens (15-60 min)
- Rotate refresh tokens on use
- Validate tokens on every protected endpoint
- Log security events (failed logins, token generation)
- Clean up expired refresh tokens periodically

### DON'T ❌
- Store JWT secret in source control
- Include sensitive data in token claims (passwords, SSNs)
- Use long-lived access tokens (>60 minutes)
- Reuse refresh tokens after validation
- Skip token validation on protected endpoints
- Allow access tokens without expiration
- Forget to revoke tokens on logout

---

## 🛠️ Generate Secure Secret

```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})

# Linux/Mac
openssl rand -base64 32

# Online (for testing only)
https://generate-secret.vercel.app/32
```

---

## 📖 Related Documentation

- [Full Implementation Guide](./JWT_TOKEN_IMPLEMENTATION.md)
- [Infrastructure Authentication Setup](./INFRASTRUCTURE_AUTH_SETUP.md)
- [Application Layer Authentication](../src/Archu.Application/docs/Authentication/README.md)
- [Architecture Guide](./ARCHITECTURE.md)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0
