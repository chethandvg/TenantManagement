# Authentication Controller - Implementation Guide

## Overview
The `AuthenticationController` provides comprehensive JWT-based authentication endpoints for user registration, login, token management, and password operations.

## ✅ Implemented Endpoints

### 1. **POST /api/v1/authentication/register**
Registers a new user account.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "userName": "johndoe"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "base64string...",
    "accessTokenExpiresAt": "2025-01-23T01:00:00Z",
    "refreshTokenExpiresAt": "2025-01-29T00:00:00Z",
    "tokenType": "Bearer",
    "user": {
      "id": "guid",
      "userName": "johndoe",
      "email": "user@example.com",
      "emailConfirmed": false,
      "roles": []
    }
  },
  "message": "Registration successful",
  "timestamp": "2025-01-22T00:00:00Z"
}
```

**Validation Rules:**
- Email: Required, valid format, max 256 characters
- Password: Required, 8-100 characters
- UserName: Required, 3-50 characters

---

### 2. **POST /api/v1/authentication/login**
Authenticates user with email and password.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "base64string...",
    "accessTokenExpiresAt": "2025-01-23T01:00:00Z",
    "refreshTokenExpiresAt": "2025-01-29T00:00:00Z",
    "tokenType": "Bearer",
    "user": {
      "id": "guid",
      "userName": "johndoe",
      "email": "user@example.com",
      "emailConfirmed": true,
      "roles": ["User", "Manager"]
    }
  },
  "message": "Login successful"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid email or password",
  "timestamp": "2025-01-22T00:00:00Z"
}
```

---

### 3. **POST /api/v1/authentication/refresh-token**
Refreshes expired access token using refresh token.

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "accessToken": "new-access-token",
    "refreshToken": "new-refresh-token",
    "accessTokenExpiresAt": "2025-01-23T02:00:00Z",
    "refreshTokenExpiresAt": "2025-01-29T01:00:00Z",
    "tokenType": "Bearer",
    "user": { /* user info */ }
  },
  "message": "Token refreshed successfully"
}
```

**When to Use:**
- Access token expires (typically after 60 minutes)
- Before access token expires (proactive refresh)
- On 401 Unauthorized response

**Token Rotation:**
- Old refresh token is invalidated
- New refresh token issued
- Enhanced security through one-time use

---

### 4. **POST /api/v1/authentication/logout**
Logs out user by revoking refresh token.

**Headers:**
```
Authorization: Bearer {access-token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "Logged out successfully"
}
```

**Client Actions After Logout:**
1. Discard access token
2. Discard refresh token
3. Redirect to login page
4. Clear authentication state

---

### 5. **POST /api/v1/authentication/change-password**
Changes authenticated user's password.

**Headers:**
```
Authorization: Bearer {access-token}
```

**Request:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword456!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "Password changed successfully"
}
```

**Validation:**
- Current password must match
- New password: 8-100 characters
- New password cannot be same as current

---

### 6. **POST /api/v1/authentication/forgot-password**
Initiates password reset process.

**Request:**
```json
{
  "email": "user@example.com"
}
```

**Response (200 OK - Always):**
```json
{
  "success": true,
  "data": {},
  "message": "If your email exists in our system, you will receive a password reset link."
}
```

**Security Features:**
- Always returns success (prevents email enumeration)
- Reset token sent only to valid emails
- Token expires after limited time (typically 1 hour)
- Single-use token

---

### 7. **POST /api/v1/authentication/reset-password**
Resets password using reset token from email.

**Request:**
```json
{
  "email": "user@example.com",
  "resetToken": "token-from-email",
  "newPassword": "NewSecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "Password reset successfully"
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Invalid or expired reset token"
}
```

---

### 8. **POST /api/v1/authentication/confirm-email**
Confirms user's email address.

**Request:**
```json
{
  "userId": "user-guid",
  "confirmationToken": "token-from-email"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "Email confirmed successfully"
}
```

---

## 🔐 Security Features

### 1. Password Security
- Hashed using PBKDF2-HMAC-SHA256
- Salted automatically
- Never stored in plain text
- Never returned in responses

### 2. JWT Token Security
- Signed with HS256 algorithm
- Contains user claims (id, email, roles)
- Short-lived (60 minutes default)
- Validated on every request

### 3. Refresh Token Security
- Cryptographically secure random string
- Longer-lived (7 days default)
- Stored in database
- Can be revoked
- One-time use (rotated on refresh)

### 4. Email Enumeration Prevention
- Forgot password always returns success
- No indication whether email exists
- Prevents account discovery

### 5. Logging & Auditing
- All authentication attempts logged
- Failed login attempts tracked
- User actions audited
- Sensitive data excluded from logs

---

## 📝 Request Validation

All endpoints use Data Annotations for automatic validation:

### RegisterRequest
```csharp
[Required] Email (max 256, valid format)
[Required] Password (8-100 characters)
[Required] UserName (3-50 characters)
```

### LoginRequest
```csharp
[Required] Email (valid format)
[Required] Password
```

### RefreshTokenRequest
```csharp
[Required] RefreshToken
```

### ChangePasswordRequest
```csharp
[Required] CurrentPassword
[Required] NewPassword (8-100 characters)
```

### ForgotPasswordRequest
```csharp
[Required] Email (valid format)
```

### ResetPasswordRequest
```csharp
[Required] Email (valid format)
[Required] ResetToken
[Required] NewPassword (8-100 characters)
```

### ConfirmEmailRequest
```csharp
[Required] UserId
[Required] ConfirmationToken
```

---

## 🚀 Usage Examples

### Complete Authentication Flow

#### 1. Register New User
```bash
curl -X POST https://localhost:7001/api/v1/authentication/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "userName": "john"
  }'
```

#### 2. Login
```bash
TOKEN_RESPONSE=$(curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!"
  }')

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.data.accessToken')
REFRESH_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.data.refreshToken')
```

#### 3. Access Protected Resource
```bash
curl -X GET https://localhost:7001/api/v1/products \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

#### 4. Refresh Token (when access token expires)
```bash
NEW_TOKEN_RESPONSE=$(curl -X POST https://localhost:7001/api/v1/authentication/refresh-token \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\": \"$REFRESH_TOKEN\"}")

ACCESS_TOKEN=$(echo $NEW_TOKEN_RESPONSE | jq -r '.data.accessToken')
REFRESH_TOKEN=$(echo $NEW_TOKEN_RESPONSE | jq -r '.data.refreshToken')
```

#### 5. Change Password
```bash
curl -X POST https://localhost:7001/api/v1/authentication/change-password \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "SecurePass123!",
    "newPassword": "NewSecurePass456!"
  }'
```

#### 6. Logout
```bash
curl -X POST https://localhost:7001/api/v1/authentication/logout \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

---

## 🎯 Client Implementation Guide

### JavaScript/TypeScript Example
```typescript
class AuthService {
  private baseUrl = 'https://api.example.com/api/v1/authentication';
  
  async register(email: string, password: string, userName: string) {
    const response = await fetch(`${this.baseUrl}/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password, userName })
    });
    
    if (response.ok) {
      const result = await response.json();
      this.storeTokens(result.data.accessToken, result.data.refreshToken);
      return result;
    }
    
    throw new Error('Registration failed');
  }
  
  async login(email: string, password: string) {
    const response = await fetch(`${this.baseUrl}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    
    if (response.ok) {
      const result = await response.json();
      this.storeTokens(result.data.accessToken, result.data.refreshToken);
      return result;
    }
    
    throw new Error('Login failed');
  }
  
  async refreshToken() {
    const refreshToken = this.getRefreshToken();
    const response = await fetch(`${this.baseUrl}/refresh-token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });
    
    if (response.ok) {
      const result = await response.json();
      this.storeTokens(result.data.accessToken, result.data.refreshToken);
      return result;
    }
    
    // Refresh failed - redirect to login
    this.clearTokens();
    window.location.href = '/login';
  }
  
  async logout() {
    const accessToken = this.getAccessToken();
    await fetch(`${this.baseUrl}/logout`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${accessToken}` }
    });
    
    this.clearTokens();
    window.location.href = '/login';
  }
  
  private storeTokens(accessToken: string, refreshToken: string) {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
  }
  
  private getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }
  
  private getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }
  
  private clearTokens() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }
}
```

---

## 🧪 Testing with Scalar UI

1. Navigate to: `https://localhost:7001/scalar/v1`
2. Find Authentication endpoints section
3. Click "Authorize" button
4. Register or login to get token
5. Enter: `Bearer {your-access-token}`
6. Click "Authorize"
7. Test all protected endpoints

---

## 📊 Response Status Codes

| Endpoint | Success | Error Codes |
|----------|---------|-------------|
| Register | 200 | 400 (validation/duplicate) |
| Login | 200 | 401 (invalid credentials) |
| Refresh Token | 200 | 401 (invalid/expired token) |
| Logout | 200 | 401 (not authenticated) |
| Change Password | 200 | 400 (wrong current password), 401 (not authenticated) |
| Forgot Password | 200 | Always 200 (security) |
| Reset Password | 200 | 400 (invalid/expired token) |
| Confirm Email | 200 | 400 (invalid/expired token) |

---

## 🔍 Troubleshooting

### "401 Unauthorized" on protected endpoints
- Check token is included in Authorization header
- Verify token format: `Bearer {token}`
- Check token hasn't expired
- Use refresh token to get new access token

### "400 Bad Request" on registration
- Check email format
- Verify password meets requirements (8+ chars)
- Ensure username is 3-50 characters
- Check if email already exists

### Token refresh fails with 401
- Refresh token expired (7 days default)
- Refresh token was revoked (logout)
- User must login again

### Password reset token invalid
- Token expired (typically 1 hour)
- Token already used (single-use)
- Request new reset token

---

## 🏗️ Architecture

### Clean Architecture Layers
```
AuthenticationController (API Layer)
        ↓
IAuthenticationService (Application Layer)
        ↓
AuthenticationService (Infrastructure Layer)
        ↓
UserRepository, JwtTokenService, PasswordHasher
```

### Dependency Injection
```csharp
// Program.cs
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
```

---

## 📚 Related Documentation

- [JWT Implementation](./JWT_IMPLEMENTATION.md)
- [Authorization Policies](./AUTHORIZATION_POLICIES.md)
- [Quick Start Guide](./QUICK_START.md)

---

**Implementation Date**: 2025-01-22  
**Version**: 1.0  
**Status**: ✅ Production Ready
