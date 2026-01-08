using TentMan.AdminApi.Authorization;
using TentMan.AdminApi.Middleware;
using TentMan.Application.Common.Behaviors;
using TentMan.Infrastructure;
using TentMan.Infrastructure.Persistence;
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
// - Repositories (UserRepository, RoleRepository, UserRoleRepository, etc.)
// - Infrastructure Services (ICurrentUser, ITimeProvider, IPasswordHasher, etc.)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Add MediatR with behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(TentMan.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(TentMan.Application.AssemblyReference).Assembly);

// ✅ Configure Admin Authorization with custom policies and handlers
builder.Services.AddAdminAuthorizationHandlers();
builder.Services.AddAuthorization(options =>
{
    options.ConfigureAdminPolicies();
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
        tags: new[] { "db", "sql", "sqlserver" });

builder.Services.AddControllers();

// ✅ Configure OpenAPI with comprehensive documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "TentMan Admin API",
            Version = "v1",
            Description = """
                ## TentMan Admin API
                
                Administrative API for managing users, roles, and system configuration.
                
                ### Features
                - 🔐 **JWT Authentication** - Secure token-based authentication
                - 👥 **User Management** - Create, read, update, and delete users
                - 🎭 **Role Management** - Manage system roles and permissions
                - 🔗 **Role Assignment** - Assign and remove roles from users
                - 🚀 **System Initialization** - Bootstrap system with roles and super admin
                - 🛡️ **Security Restrictions** - Role-based access control with privilege escalation protection
                
                ### Security
                All endpoints require JWT authentication. Include the token in the `Authorization` header:
                ```
                Authorization: Bearer <your-jwt-token>
                ```
                
                ### Role Hierarchy
                - **SuperAdmin** - Full system access, can manage all users and roles
                - **Administrator** - Can manage users and assign non-privileged roles
                - **Manager** - Can view users and roles, limited management capabilities
                - **User** - Standard user access
                - **Guest** - Read-only access
                
                ### Important Notes
                - SuperAdmin role can only be assigned by another SuperAdmin
                - Administrator role can only be assigned by SuperAdmin
                - Cannot delete the last SuperAdmin in the system
                - Cannot remove your own privileged roles (SuperAdmin/Administrator)
                
                ### Getting Started
                1. Initialize the system using `/api/v1/admin/initialization/initialize`
                2. Authenticate to get a JWT token
                3. Use the token to access protected endpoints
                
                For detailed documentation, visit: [Admin API Documentation](https://github.com/chethandvg/tentman/tree/main/docs)
                """,
            Contact = new()
            {
                Name = "TentMan API Support",
                Email = "support@tentman.com",
                Url = new Uri("https://github.com/chethandvg/tentman")
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
                    
                    Enter your JWT token in the text input below.
                    
                    Example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                    
                    Note: Do NOT include the "Bearer " prefix - it will be added automatically.
                    """
            }
        };

        // Apply security requirement globally
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
            new() { Url = "https://localhost:7290", Description = "Local Development (HTTPS)" },
            new() { Url = "http://localhost:5290", Description = "Local Development (HTTP)" }
        };

        // Add tags with descriptions
        document.Tags = new List<OpenApiTag>
        {
            new()
            {
                Name = "Initialization",
                Description = "System initialization and setup endpoints. Used for bootstrapping the application."
            },
            new()
            {
                Name = "Users",
                Description = "User management operations. Create, read, update, and delete users. **Requires Admin access.**"
            },
            new()
            {
                Name = "Roles",
                Description = "Role management operations. Create, read, update, and delete roles. **Requires Admin access.**"
            },
            new()
            {
                Name = "UserRoles",
                Description = "User-role assignment operations. Assign and remove roles from users. **Security restrictions apply.**"
            },
            new()
            {
                Name = "Health",
                Description = "Health check endpoints for monitoring application status."
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
        options.Title = "TentMan Admin API";
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.Theme = ScalarTheme.Purple;
        options.ShowSidebar = true;
        options.DarkMode = false;
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
