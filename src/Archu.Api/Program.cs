using System.Text;
using Archu.Api.Authorization;
using Archu.Api.Health;
using Archu.Api.Middleware;
using Archu.Application.Common.Behaviors;
using Archu.Infrastructure;
using Archu.Infrastructure.Persistence;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults for Aspire (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddHttpContextAccessor();

// ✅ Use Infrastructure DependencyInjection extension
// This registers:
// - Database (ApplicationDbContext)
// - JWT Authentication (JwtTokenService, JwtOptions, JWT Bearer middleware)
// - Repositories (ProductRepository, UserRepository, etc.)
// - Infrastructure Services (ICurrentUser, ITimeProvider, etc.)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Add MediatR with behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Archu.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Archu.Application.AssemblyReference).Assembly);

// Configure Authorization with custom policies
builder.Services.AddAuthorizationHandlers(); // Register custom authorization handlers
builder.Services.AddAuthorization(options =>
{
    options.ConfigureArchuPolicies(); // Configure all application policies
});

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Add Health Checks
var connectionString = builder.Configuration.GetConnectionString("archudb")
    ?? builder.Configuration.GetConnectionString("Sql")
    ?? throw new InvalidOperationException("Database connection string not configured");

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString,
        healthQuery: "SELECT 1;",
        name: "sql-server",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "sqlserver" })
    .AddCheck<DatabaseHealthCheck>(
        "application-db-context",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "ef-core" });

builder.Services.AddScoped<DatabaseHealthCheck>();

builder.Services.AddControllers();

// ✅ Configure OpenAPI with comprehensive documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Archu API",
            Version = "v1",
            Description = """
                ## Archu API
                
                Comprehensive API for authentication, product management, and application services.
                
                ### Features
                - 🔐 **JWT Authentication** - Secure token-based authentication with refresh tokens
                - 👤 **User Registration & Login** - Complete user account management
                - 🔑 **Password Management** - Change, reset, and forgot password workflows
                - ✉️ **Email Verification** - Confirm user email addresses
                - 📦 **Product Management** - Full CRUD operations for product catalog
                - 🛡️ **Role-Based Authorization** - Fine-grained access control with custom policies
                - 🔄 **Token Refresh** - Seamless token renewal without re-authentication
                - 🏥 **Health Checks** - Monitor application and database status
                
                ### Authentication
                Most endpoints require JWT authentication. Include the token in the `Authorization` header:
                ```
                Authorization: Bearer <your-jwt-token>
                ```
                
                ### Getting Started
                1. **Register** a new account using `/api/v1/authentication/register`
                2. **Login** to get JWT access and refresh tokens
                3. Use the **access token** for authenticated requests
                4. **Refresh** tokens before expiration using `/api/v1/authentication/refresh-token`
                
                ### Token Lifetimes
                - **Access Token**: 1 hour (default)
                - **Refresh Token**: 7 days (default)
                
                ### Role-Based Access
                Different endpoints require different roles:
                - **Public**: No authentication required (register, login)
                - **User**: Standard authenticated access (read products)
                - **Manager**: Elevated access (create, update products)
                - **Admin**: Full access (delete products, all operations)
                
                ### Password Requirements
                - Minimum 8 characters
                - Maximum 100 characters
                - Additional complexity requirements may apply
                
                ### API Response Format
                All endpoints return standardized `ApiResponse<T>` wrapper:
                ```json
                {
                  "success": true,
                  "message": "Operation completed successfully",
                  "data": { /* response data */ }
                }
                ```
                
                ### Error Handling
                Errors follow consistent format:
                ```json
                {
                  "success": false,
                  "message": "Error description",
                  "data": null
                }
                ```
                
                ### Common Status Codes
                - **200 OK**: Success
                - **201 Created**: Resource created successfully
                - **400 Bad Request**: Validation error or invalid request
                - **401 Unauthorized**: Missing or invalid authentication
                - **403 Forbidden**: Insufficient permissions
                - **404 Not Found**: Resource doesn't exist
                - **409 Conflict**: Concurrency conflict (optimistic locking)
                
                ### Workflow Examples
                
                **Complete Registration Flow:**
                1. POST `/api/v1/authentication/register` - Create account
                2. POST `/api/v1/authentication/confirm-email` - Verify email (optional)
                3. Use tokens from registration response
                
                **Password Reset Flow:**
                1. POST `/api/v1/authentication/forgot-password` - Request reset token
                2. Check email for reset token
                3. POST `/api/v1/authentication/reset-password` - Reset with token
                
                **Token Refresh Flow:**
                1. Detect access token expiration (401 error)
                2. POST `/api/v1/authentication/refresh-token` - Get new tokens
                3. Retry original request with new access token
                
                ### Additional Resources
                - **GitHub Repository**: [https://github.com/chethandvg/archu](https://github.com/chethandvg/archu)
                - **HTTP Test File**: `src/Archu.Api/Archu.Api.http` (40+ example requests)
                - **Documentation**: `/docs` folder in repository
                
                ### Support
                For questions or issues, please visit our GitHub repository or contact support.
                """,
            Contact = new()
            {
                Name = "Archu API Support",
                Email = "support@archu.com",
                Url = new Uri("https://github.com/chethandvg/archu")
            },
            License = new()
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        // Add security scheme for JWT Bearer token
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new()
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = """
                    JWT Authorization header using the Bearer scheme.
                    
                    **How to authenticate:**
                    1. Register or login to get a JWT token
                    2. Copy the token value from the response
                    3. Click "Authorize" button above
                    4. Paste token in the input field (without "Bearer " prefix)
                    5. Click "Authorize" to apply
                    
                    **Token Format:**
                    ```
                    eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
                    ```
                    
                    **Note:** The "Bearer " prefix is added automatically.
                    
                    **Token Expiration:**
                    - Access tokens expire after 1 hour
                    - Use `/api/v1/authentication/refresh-token` to get new tokens
                    - Refresh tokens are valid for 7 days
                    """
            }
        };

        // Apply security requirement globally (endpoints can override with [AllowAnonymous])
        document.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new()
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            }
        };

        // Add server information
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "https://localhost:7268", Description = "Local Development (HTTPS)" },
            new() { Url = "http://localhost:5268", Description = "Local Development (HTTP)" }
        };

        // Add tags with descriptions for better organization
        document.Tags = new List<OpenApiTag>
        {
            new()
            {
                Name = "Authentication",
                Description = """
                    User authentication and account management endpoints.
                    
                    **Public Endpoints** (No auth required):
                    - Register new account
                    - Login with credentials
                    - Refresh expired tokens
                    - Request password reset
                    - Reset password with token
                    - Confirm email address
                    
                    **Protected Endpoints** (Auth required):
                    - Change password
                    - Logout (revoke refresh token)
                    """
            },
            new()
            {
                Name = "Products",
                Description = """
                    Product catalog management with role-based access control.
                    
                    **Access Levels:**
                    - **Read (GET)**: All authenticated users
                    - **Create (POST)**: Manager and Admin roles only
                    - **Update (PUT)**: Manager and Admin roles only
                    - **Delete (DELETE)**: Admin role only
                    
                    **Features:**
                    - Optimistic concurrency control (RowVersion)
                    - Soft delete support
                    - Full CRUD operations
                    """
            },
            new()
            {
                Name = "Health",
                Description = """
                    Health check endpoints for monitoring application status.
                    
                    **Endpoints:**
                    - `/health` - Full health status (all checks)
                    - `/health/ready` - Readiness check (ready to accept traffic)
                    - `/health/live` - Liveness check (application is running)
                    
                    **Checked Components:**
                    - SQL Server connection
                    - Entity Framework Core DbContext
                    - Application dependencies
                    """
            }
        };

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Add Global Exception Handler - Must be first in pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Archu API";
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.Theme = ScalarTheme.DeepSpace;
        options.ShowSidebar = true;
        options.DarkMode = true;
    });
}

app.UseHttpsRedirection();

// Authentication & Authorization Middleware (ORDER MATTERS!)
app.UseAuthentication(); // First: Identify who you are
app.UseAuthorization();  // Second: Check what you can do

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString(),
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsJsonAsync(response);
    }
}).WithTags("Health");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).WithTags("Health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Only checks that the app is running
}).WithTags("Health");

app.MapDefaultEndpoints();

app.Run();
