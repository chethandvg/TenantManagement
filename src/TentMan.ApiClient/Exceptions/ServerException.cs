namespace TentMan.ApiClient.Exceptions;

/// <summary>
/// Exception thrown when a server error occurs (HTTP 5xx).
/// </summary>
public sealed class ServerException : ApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerException"/> class.
    /// </summary>
    public ServerException(string message, int statusCode)
        : base(message, statusCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerException"/> class.
    /// </summary>
    public ServerException(string message, int statusCode, Exception innerException)
        : base(message, statusCode, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerException"/> class.
    /// </summary>
    public ServerException(string message, int statusCode, IEnumerable<string>? errors)
        : base(message, statusCode, errors)
    {
    }
}
