using Archu.Api.Auth;
using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Archu.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults for Aspire (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>(); // implement per your auth
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITimeProvider, SystemTimeProvider>();

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("archudb") ?? builder.Configuration.GetConnectionString("Sql"),
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.CommandTimeout(30);
        }));

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

app.MapDefaultEndpoints();

app.Run();
