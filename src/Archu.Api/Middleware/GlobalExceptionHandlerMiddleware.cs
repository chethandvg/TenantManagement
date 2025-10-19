using FluentValidation;
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
        var (statusCode, message, details, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred",
                "Validation failed",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The resource was modified by another user. Please reload and try again.",
                "Concurrency conflict occurred",
                null
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "The requested resource was not found.",
                exception.Message,
                null
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "You do not have permission to access this resource.",
                exception.Message,
                null
            ),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request parameters.",
                exception.Message,
                null
            ),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "The operation is invalid in the current state.",
                exception.Message,
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.",
                exception.Message,
                null
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
            Errors = errors,
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
        public IEnumerable<string>? Errors { get; init; }
        public string? StackTrace { get; init; }
        public string? TraceId { get; init; }
    }
}
