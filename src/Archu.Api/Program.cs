using Archu.Api.Auth;
using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Archu.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>(); // implement per your auth
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITimeProvider, SystemTimeProvider>();

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Sql"),
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.CommandTimeout(30);
        }));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScalar();

var app = builder.Build();

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

app.Run();
