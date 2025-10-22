using Archu.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Archu.AdminApi.Middleware;

/// <summary>
/// Global exception handler middleware for Admin API.
/// Catches unhandled exceptions and returns consistent error responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            InvalidOperationException => CreateResponse(
                HttpStatusCode.BadRequest,
                exception.Message),

            UnauthorizedAccessException => CreateResponse(
                HttpStatusCode.Unauthorized,
                "Unauthorized access"),

            DbUpdateConcurrencyException => CreateResponse(
                HttpStatusCode.Conflict,
                "The record was modified by another user. Please refresh and try again."),

            DbUpdateException dbEx => CreateResponse(
                HttpStatusCode.BadRequest,
                "Database update failed. Please check your input and try again.",
                _environment.IsDevelopment() ? dbEx.InnerException?.Message : null),

            _ => CreateResponse(
                HttpStatusCode.InternalServerError,
                "An error occurred while processing your request.",
                _environment.IsDevelopment() ? exception.Message : null)
        };

        context.Response.StatusCode = (int)response.StatusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response.ApiResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static (HttpStatusCode StatusCode, ApiResponse<object> ApiResponse) CreateResponse(
        HttpStatusCode statusCode,
        string message,
        string? detail = null)
    {
        var errors = detail != null ? new[] { detail } : null;

        return (statusCode, ApiResponse<object>.Fail(message, errors));
    }
}
