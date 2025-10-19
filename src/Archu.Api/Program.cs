using Archu.Api.Auth;
using Archu.Api.Health;
using Archu.Api.Middleware;
using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Archu.Infrastructure.Repositories;
using Archu.Infrastructure.Time;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults for Aspire (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>(); // implement per your auth
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITimeProvider, SystemTimeProvider>();

// Register repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("archudb") ?? builder.Configuration.GetConnectionString("Sql"),
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.CommandTimeout(30);
        }));

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

app.UseAuthorization();

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
