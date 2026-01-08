namespace TentMan.ApiClient.Exceptions;

/// <summary>
/// Exception thrown when a validation error occurs (HTTP 400).
/// </summary>
public sealed class ValidationException : ApiClientException
{
    /// <summary>
    /// Gets the validation errors grouped by field name.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException(string message, IEnumerable<string>? errors = null)
        : base(message, 400, errors)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException(string message, IDictionary<string, string[]> validationErrors)
        : base(message, 400, validationErrors.SelectMany(kvp => kvp.Value))
    {
        ValidationErrors = validationErrors.AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException(string message, Exception innerException)
        : base(message, 400, innerException)
    {
    }
}
