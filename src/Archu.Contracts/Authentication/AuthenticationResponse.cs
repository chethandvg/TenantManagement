namespace Archu.Contracts.Authentication;

/// <summary>
/// DTO for authentication response containing tokens and user information.
/// </summary>
public sealed record AuthenticationResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// The refresh token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// The access token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// The token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// User information.
    /// </summary>
    public UserInfoDto User { get; init; } = null!;
}

/// <summary>
/// DTO for user information.
/// </summary>
public sealed record UserInfoDto
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The user's username.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the user's email is confirmed.
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    public IEnumerable<string> Roles { get; init; } = Array.Empty<string>();
}
