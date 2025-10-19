using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Archu.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches and formats exceptions.
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, details) = exception switch
        {
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The resource was modified by another user. Please reload and try again.",
                "Concurrency conflict occurred"
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "The requested resource was not found.",
                exception.Message
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "You do not have permission to access this resource.",
                exception.Message
            ),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request parameters.",
                exception.Message
            ),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "The operation is invalid in the current state.",
                exception.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.",
                exception.Message
            )
        };

        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Details = _environment.IsDevelopment() ? details : null,
            StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
            TraceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }

    private record ErrorResponse
    {
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? Details { get; init; }
        public string? StackTrace { get; init; }
        public string? TraceId { get; init; }
    }
}
