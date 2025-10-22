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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
