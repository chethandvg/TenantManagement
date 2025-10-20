using Archu.Api.Authorization;
using Archu.Api.Health;
using Archu.Api.Middleware;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common.Behaviors;
using Archu.Infrastructure.Authentication;
using Archu.Infrastructure.Persistence;
using Archu.Infrastructure.Repositories;
using Archu.Infrastructure.Time;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults for Aspire (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>(); // Use Infrastructure implementation
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITimeProvider, SystemTimeProvider>();

// Register authentication services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Configure JWT options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// Validate JWT options on startup
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing");
jwtOptions.Validate();

// Register repositories and Unit of Work
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Sql") ?? builder.Configuration.GetConnectionString("archudb"),
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.CommandTimeout(30);
        }));

// Add MediatR with behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Archu.Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Archu.Application.AssemblyReference).Assembly);

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(jwtOptions.Secret);
    
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Allow HTTP in development
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // No tolerance for token expiration
        
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    
    // Configure event handlers for better logging and debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            
            logger.LogWarning(
                context.Exception,
                "JWT authentication failed: {Message}",
                context.Exception.Message);
            
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            logger.LogDebug("JWT token validated successfully for user: {UserId}", userId);
            
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            
            logger.LogWarning(
                "JWT authentication challenge: {Error} - {ErrorDescription}",
                context.Error,
                context.ErrorDescription);
            
            return Task.CompletedTask;
        }
    };
});

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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations in development
//if (app.Environment.IsDevelopment())
//{
//    try
//    {
//        using var scope = app.Services.CreateScope();
//        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

//        logger.LogInformation("Applying database migrations...");
//        await dbContext.Database.MigrateAsync();
//        logger.LogInformation("Database migrations applied successfully");
//    }
//    catch (Exception ex)
//    {
//        var logger = app.Services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "An error occurred while applying database migrations");
//        // Decide whether to throw or continue
//        throw;
//    }
//}

/*# Example Azure DevOps/GitHub Actions step
- name: Apply EF Core Migrations
  run: dotnet ef database update --project src/Archu.Infrastructure
*/

// Add Global Exception Handler - Must be first in pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Archu API";
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
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Only checks that the app is running
});

app.MapDefaultEndpoints();

app.Run();
