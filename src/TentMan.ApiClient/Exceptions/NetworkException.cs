namespace TentMan.ApiClient.Exceptions;

/// <summary>
/// Exception thrown when a network-related error occurs.
/// </summary>
public sealed class NetworkException : ApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    public NetworkException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    public NetworkException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
