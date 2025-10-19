namespace Archu.Application.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public record Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value returned on success.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the error message returned on failure.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    public IEnumerable<string>? Errors { get; init; }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static Result<T> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    public static Result<T> Failure(string error, IEnumerable<string> errors) => new()
    {
        IsSuccess = false,
        Error = error,
        Errors = errors
    };
}

/// <summary>
/// Represents the result of an operation that doesn't return a value.
/// </summary>
public record Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message returned on failure.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    public IEnumerable<string>? Errors { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static Result Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    public static Result Failure(string error, IEnumerable<string> errors) => new()
    {
        IsSuccess = false,
        Error = error,
        Errors = errors
    };
}
