namespace TentMan.Contracts.Authorization;

/// <summary>
/// Response for authorization check requests.
/// </summary>
public sealed class AuthorizationCheckResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the authorization check passed.
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// Gets or sets an optional message explaining the authorization decision.
    /// </summary>
    public string? Message { get; set; }
}
