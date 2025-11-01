# Archu API

Main API for the Archu application providing authentication, product management, and core business services.

## Overview

The Archu API is built using ASP.NET Core 9.0 and follows Clean Architecture principles with CQRS pattern. It provides:

- **🔐 Authentication & Authorization** - JWT-based authentication with refresh tokens
- **👤 User Account Management** - Registration, login, password management, email verification
- **📦 Product Catalog** - Full CRUD operations with role-based access control
- **🏥 Health Monitoring** - Application and database health checks
- **📊 API Documentation** - Comprehensive OpenAPI/Scalar documentation

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server (local or Docker)
- Visual Studio 2022 or VS Code with C# DevKit

### Quick Start

1. **Configure Database Connection**

   Update `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "archudb": "Server=localhost;Database=ArchuDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

2. **Run Database Migrations**

   ```bash
   cd src/Archu.Infrastructure
   dotnet ef database update --startup-project ../Archu.Api
   ```

3. **Start the API**

   ```bash
 cd src/Archu.Api
   dotnet run
   ```

4. **Access API Documentation**

   Open your browser to:
   - **Scalar UI**: `https://localhost:7123/scalar/v1`
   - **OpenAPI JSON**: `https://localhost:7123/openapi/v1.json`

## API Endpoints

### Authentication

All authentication endpoints are public (no JWT required):

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/authentication/register` | Register new user account |
| POST | `/api/v1/authentication/login` | Login with email/password |
| POST | `/api/v1/authentication/refresh-token` | Refresh expired access token |
| POST | `/api/v1/authentication/logout` | Logout and invalidate refresh token |
| POST | `/api/v1/authentication/change-password` | Change password (authenticated) |
| POST | `/api/v1/authentication/forgot-password` | Request password reset token |
| POST | `/api/v1/authentication/reset-password` | Reset password with token |
| POST | `/api/v1/authentication/confirm-email` | Confirm email address |

### Products

Product endpoints require authentication and specific roles:

| Method | Endpoint | Required Role | Description |
|--------|----------|---------------|-------------|
| GET | `/api/v1/products` | User+ | List all products (paginated) |
| GET | `/api/v1/products/{id}` | User+ | Get product by ID |
| POST | `/api/v1/products` | Manager+ | Create new product |
| PUT | `/api/v1/products/{id}` | Manager+ | Update existing product |
| DELETE | `/api/v1/products/{id}` | Admin | Delete product (soft delete) |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Full health status (all checks) |
| GET | `/health/ready` | Readiness probe |
| GET | `/health/live` | Liveness probe |

## Authentication Flow

### 1. Registration

```bash
POST /api/v1/authentication/register
Content-Type: application/json

{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "userId": "guid-here",
    "userName": "johndoe",
    "email": "john@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-here",
    "expiresAt": "2025-01-23T12:00:00Z"
  }
}
```

### 2. Login

```bash
POST /api/v1/authentication/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

### 3. Using JWT Token

Include the access token in the Authorization header:

```bash
GET /api/v1/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 4. Refreshing Tokens

When access token expires (default: 1 hour):

```bash
POST /api/v1/authentication/refresh-token
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

## Role-Based Access Control

The API uses a hierarchical role system:

| Role | Permissions |
|------|-------------|
| **Guest** | Read-only access to public resources |
| **User** | Can view products, manage own account |
| **Manager** | Can create/update products, User permissions |
| **Administrator** | Can delete products, all lower permissions |
| **SuperAdmin** | Full system access (managed via Admin API) |

## Configuration

### JWT Settings

Located in `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Production:** Use environment variables or Azure Key Vault:
```bash
export Jwt__Secret="production-secret-here"
```

### CORS Configuration

Development mode allows all origins for Blazor WebAssembly:

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

**Production:** Restrict to specific origins.

## Testing the API

### Using Scalar UI (Recommended)

1. Run the API
2. Navigate to `https://localhost:7123/scalar/v1`
3. Click "Authorize" and paste your JWT token
4. Test endpoints interactively

### Using HTTP Files

The repository includes `Archu.Api.http` with 40+ example requests:

```bash
# Open in Visual Studio
# File location: src/Archu.Api/Archu.Api.http
# Click "Send Request" next to any request
```

### Using curl

```bash
# Register
curl -X POST https://localhost:7123/api/v1/authentication/register \
  -H "Content-Type: application/json" \
  -d '{"userName":"test","email":"test@example.com","password":"Test123!"}'

# Login
curl -X POST https://localhost:7123/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Get Products (with token)
curl -X GET https://localhost:7123/api/v1/products \
  -H "Authorization: Bearer your-jwt-token"
```

## Architecture

The API follows Clean Architecture principles:

```
Archu.Api (Presentation Layer)
    ↓ Controllers
Archu.Application (Business Logic)
    ↓ CQRS (Commands/Queries)
Archu.Infrastructure (Data Access)
    ↓ Repositories, DbContext
Archu.Domain (Core Business Entities)
```

### Project Structure

```
Archu.Api/
├── Controllers/
│   ├── AuthenticationController.cs    # User authentication & account management
│   └── ProductsController.cs          # Product catalog CRUD operations
├── Authorization/
│   ├── AuthorizationHandlerExtensions.cs
│   ├── AuthorizationPolicies.cs  # Policy definitions
│   ├── AuthorizationPolicyExtensions.cs
│   ├── PolicyNames.cs           # Policy name constants
│   ├── Requirements/
│   │   ├── AuthorizationRequirements.cs
│   │   └── ResourceOwnerRequirement.cs
│   └── Handlers/
│       └── ResourceOwnerRequirementHandler.cs
├── Health/
│ └── DatabaseHealthCheck.cs      # EF Core health check
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs  # Centralized error handling
├── Program.cs                 # Application startup & configuration
├── appsettings.json            # Configuration
├── appsettings.Development.json
├── Archu.Api.http    # HTTP request examples
└── README.md  # This file
```

## API Response Format

All endpoints return a standardized response wrapper:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    // Response data here
  }
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

## Common HTTP Status Codes

| Code | Meaning | When It Occurs |
|------|---------|----------------|
| 200 | OK | Successful GET, PUT, DELETE |
| 201 | Created | Successful POST (resource created) |
| 400 | Bad Request | Validation error or invalid request |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | Insufficient permissions for action |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Concurrency conflict (optimistic locking) |
| 500 | Server Error | Unexpected server error |

## Security Best Practices

### 1. JWT Token Management

- **Access tokens**: Short-lived (1 hour default)
- **Refresh tokens**: Longer-lived (7 days default)
- Store tokens securely (avoid localStorage for sensitive apps)
- Implement token refresh before expiration
- Clear tokens on logout

### 2. Password Requirements

- Minimum 8 characters
- Maximum 100 characters
- Additional complexity enforced by ASP.NET Core Identity

### 3. Production Deployment

- Use HTTPS only
- Store secrets in Azure Key Vault or environment variables
- Enable rate limiting
- Configure proper CORS policies
- Monitor authentication failures

## Troubleshooting

### "Unable to connect to database"

**Solution:** Verify connection string and ensure SQL Server is running:
```bash
# Test connection
dotnet ef database update --startup-project src/Archu.Api
```

### "JWT Secret is not configured"

**Solution:** Set JWT secret in `appsettings.Development.json` or environment variables:
```json
{
  "Jwt": {
    "Secret": "your-secret-at-least-32-characters-long"
  }
}
```

### "401 Unauthorized on protected endpoints"

**Solutions:**
1. Ensure JWT token is included in Authorization header
2. Verify token hasn't expired
3. Use refresh token to get new access token
4. Check token format: `Bearer <token>`

### "403 Forbidden"

**Solution:** User doesn't have required role. Check role assignments in Admin API.

## Development Workflows

### Adding New Endpoints

1. Create request/response DTOs in `Archu.Contracts`
2. Create command/query in `Archu.Application`
3. Add controller endpoint in `Archu.Api/Controllers`
4. Add authorization policy if needed
5. Test with HTTP file or Scalar UI

### Database Changes

1. Modify entities in `Archu.Domain`
2. Update DbContext in `Archu.Infrastructure`
3. Create migration:
   ```bash
   cd src/Archu.Infrastructure
   dotnet ef migrations add MigrationName --startup-project ../Archu.Api
   ```
4. Apply migration:
   ```bash
   dotnet ef database update --startup-project ../Archu.Api
   ```

## Related Projects

- **Archu.AdminApi** - Administrative API for user/role management
- **Archu.Ui** - Blazor WebAssembly frontend
- **Archu.ApiClient** - Typed HTTP client for API consumption
- **Archu.Application** - Business logic and CQRS handlers
- **Archu.Infrastructure** - Data access and infrastructure services
- **Archu.Domain** - Core business entities and interfaces

## Additional Resources

- **OpenAPI Documentation**: Available at `/scalar/v1` when running
- **HTTP Request Examples**: `Archu.Api.http` (40+ examples)
- **GitHub Repository**: [https://github.com/chethandvg/archu](https://github.com/chethandvg/archu)
- **Clean Architecture**: [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

---

**Version**: 1.0  
**Last Updated**: 2025-01-23  
**Target Framework**: .NET 9.0  
**License**: MIT
