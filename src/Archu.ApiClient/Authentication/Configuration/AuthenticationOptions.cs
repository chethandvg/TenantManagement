namespace Archu.ApiClient.Authentication.Configuration;

/// <summary>
/// Configuration options for authentication.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Authentication";

    /// <summary>
    /// Gets or sets a value indicating whether to automatically attach tokens to requests.
    /// </summary>
    public bool AutoAttachToken { get; set; } = true;

    /// <summary>
    /// Gets or sets the token expiration buffer in seconds.
    /// Tokens will be considered expired this many seconds before actual expiration.
    /// </summary>
    public int TokenExpirationBufferSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically refresh tokens.
    /// </summary>
    public bool AutoRefreshToken { get; set; } = true;

    /// <summary>
    /// Gets or sets the token refresh threshold in seconds.
    /// Token refresh will be attempted when remaining token lifetime is less than this value.
    /// </summary>
    public int TokenRefreshThresholdSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the authentication endpoint path.
    /// </summary>
    public string AuthenticationEndpoint { get; set; } = "api/auth/login";

    /// <summary>
    /// Gets or sets the token refresh endpoint path.
    /// </summary>
    public string RefreshTokenEndpoint { get; set; } = "api/auth/refresh";

    /// <summary>
    /// Gets or sets a value indicating whether to use browser storage (local storage) for tokens.
    /// Set to false to use in-memory storage (suitable for server-side Blazor).
    /// </summary>
    public bool UseBrowserStorage { get; set; } = false;
}
