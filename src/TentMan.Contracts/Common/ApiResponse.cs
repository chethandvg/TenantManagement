namespace TentMan.Contracts.Common;

/// <summary>
/// Represents a standardized API response wrapper.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the response data.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the message associated with the response.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the collection of error messages, if any.
    /// </summary>
    public IEnumerable<string>? Errors { get; init; }

    /// <summary>
    /// Gets the timestamp of the response.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    /// <summary>
    /// Creates a failed response with an error message.
    /// </summary>
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}
