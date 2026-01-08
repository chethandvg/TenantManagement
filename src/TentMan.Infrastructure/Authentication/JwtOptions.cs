namespace TentMan.Infrastructure.Authentication;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// These settings should be configured in appsettings.json.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The secret key used to sign JWT tokens.
    /// IMPORTANT: Must be at least 256 bits (32 characters) for HS256 algorithm.
    /// Should be stored securely (Azure Key Vault, environment variables, etc.).
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// The token issuer (typically your application's URL or name).
    /// Example: "https://api.tentman.com" or "TentMan.Api"
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// The token audience (who the token is intended for).
    /// Can be the same as Issuer for single-app scenarios.
    /// Example: "https://api.tentman.com" or "TentMan.Client"
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes.
    /// Recommended: 15-60 minutes for security.
    /// Default: 60 minutes (1 hour)
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Refresh token expiration time in days.
    /// Recommended: 7-30 days for balance between security and UX.
    /// Default: 7 days
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;

    /// <summary>
    /// Validates that the JWT options are properly configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
            throw new InvalidOperationException("JWT Secret is not configured. Please set Jwt:Secret in appsettings.json");

        if (Secret.Length < 32)
            throw new InvalidOperationException("JWT Secret must be at least 32 characters (256 bits) for security.");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer is not configured. Please set Jwt:Issuer in appsettings.json");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience is not configured. Please set Jwt:Audience in appsettings.json");

        if (AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("JWT AccessTokenExpirationMinutes must be greater than 0");

        if (RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("JWT RefreshTokenExpirationDays must be greater than 0");
    }
}
