namespace TentMan.AdminApiClient.Exceptions;

/// <summary>
/// Exception thrown when request validation fails (400 Bad Request).
/// </summary>
public class ValidationException : AdminApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errors">Optional collection of validation error messages.</param>
    public ValidationException(string message, IEnumerable<string>? errors = null)
        : base(message, 400, errors)
    {
    }
}
