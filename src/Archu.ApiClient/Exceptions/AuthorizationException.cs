namespace Archu.ApiClient.Exceptions;

/// <summary>
/// Exception thrown when an authorization error occurs (HTTP 401 or 403).
/// </summary>
public sealed class AuthorizationException : ApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
    /// </summary>
    public AuthorizationException(string message, int statusCode = 401)
        : base(message, statusCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
    /// </summary>
    public AuthorizationException(string message, int statusCode, Exception innerException)
        : base(message, statusCode, innerException)
    {
    }
}
