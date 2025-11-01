using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Archu.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Implementation of JWT token generation and validation service.
/// Uses HS256 algorithm for signing tokens.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _jwtOptions.Validate(); // Validate configuration on startup

        _tokenHandler = new JwtSecurityTokenHandler();

        // Create signing credentials with HS256 algorithm
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        var securityKey = new SymmetricSecurityKey(key);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Configure token validation parameters
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.Zero // No tolerance for expiration time
        };
    }

    /// <inheritdoc />
    public string GenerateAccessToken(
        string userId,
        string email,
        string userName,
        IEnumerable<string> roles,
        IEnumerable<Claim>? additionalClaims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        ArgumentException.ThrowIfNullOrWhiteSpace(userName, nameof(userName));

        var claims = new List<Claim>
        {
            // Standard JWT claims
            new(JwtRegisteredClaimNames.Sub, userId),           // Subject (user ID)
            new(JwtRegisteredClaimNames.Email, email),          // Email
            new(JwtRegisteredClaimNames.UniqueName, userName),  // Unique name
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID (unique identifier)
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64), // Issued at

            // Custom claims
            new(ClaimTypes.NameIdentifier, userId),  // For backward compatibility
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, email)
        };

        // Add role claims
        foreach (var role in roles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role)); // OIDC standard claim
            }
        }

        // ✅ Workflow-aligned comment: ensure optional claims (permissions, flags, etc.)
        // are appended so downstream authorization handlers receive the required data.
        if (additionalClaims is not null)
        {
            foreach (var claim in additionalClaims)
            {
                if (claim is not null)
                {
                    claims.Add(claim);
                }
            }
        }

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = _signingCredentials,
            IssuedAt = now,
            NotBefore = now // Token valid immediately
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random string
        // Refresh tokens are NOT JWTs - they're just random identifiers
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Convert to base64 for easy storage and transmission
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = _tokenHandler.ValidateToken(
                token,
                _validationParameters,
                out var validatedToken);

            // Ensure the token is a JWT with the correct algorithm
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            // Token validation failed (expired, invalid signature, etc.)
            return null;
        }
        catch (Exception)
        {
            // Unexpected error during validation
            return null;
        }
    }

    /// <inheritdoc />
    public TimeSpan GetAccessTokenExpiration()
    {
        return TimeSpan.FromMinutes(_jwtOptions.AccessTokenExpirationMinutes);
    }

    /// <inheritdoc />
    public TimeSpan GetRefreshTokenExpiration()
    {
        return TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationDays);
    }

    /// <inheritdoc />
    public DateTime GetAccessTokenExpiryUtc()
    {
        return DateTime.UtcNow.Add(GetAccessTokenExpiration());
    }

    /// <inheritdoc />
    public DateTime GetRefreshTokenExpiryUtc()
    {
        return DateTime.UtcNow.Add(GetRefreshTokenExpiration());
    }
}
