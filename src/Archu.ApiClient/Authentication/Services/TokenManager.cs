using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Archu.ApiClient.Authentication.Models;
using Archu.ApiClient.Authentication.Storage;
using Microsoft.Extensions.Logging;

namespace Archu.ApiClient.Authentication.Services;

/// <summary>
/// Interface for managing authentication tokens.
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// Gets a value indicating whether the user is currently authenticated.
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current access token if available and valid.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new token.
    /// </summary>
    Task StoreTokenAsync(TokenResponse tokenResponse, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored token (logout).
    /// </summary>
    Task RemoveTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    Task<AuthenticationState> GetAuthenticationStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts claims from a JWT token.
    /// </summary>
    ClaimsPrincipal? ExtractClaimsFromToken(string accessToken);
}

/// <summary>
/// Default implementation of token management.
/// </summary>
public sealed class TokenManager : ITokenManager
{
    private readonly ITokenStorage _tokenStorage;
    private readonly ILogger<TokenManager> _logger;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    public TokenManager(ITokenStorage tokenStorage, ILogger<TokenManager> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _tokenStorage.GetTokenAsync(cancellationToken);
            return token != null && !token.IsExpired();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _tokenStorage.GetTokenAsync(cancellationToken);

            if (token == null)
            {
                _logger.LogDebug("No token found in storage");
                return null;
            }

            if (token.IsExpired())
            {
                _logger.LogWarning("Token is expired");
                // TODO: Implement token refresh logic here if refresh token is available
                return null;
            }

            return token.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task StoreTokenAsync(TokenResponse tokenResponse, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = StoredToken.FromTokenResponse(tokenResponse);
            await _tokenStorage.StoreTokenAsync(storedToken, cancellationToken);
            _logger.LogInformation("Token stored successfully, expires at {ExpiresAt}", storedToken.ExpiresAtUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing token");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _tokenStorage.RemoveTokenAsync(cancellationToken);
            _logger.LogInformation("Token removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing token");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationState> GetAuthenticationStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _tokenStorage.GetTokenAsync(cancellationToken);

            if (token == null || token.IsExpired())
            {
                return Models.AuthenticationState.Unauthenticated();
            }

            var claimsPrincipal = ExtractClaimsFromToken(token.AccessToken);

            if (claimsPrincipal == null)
            {
                _logger.LogWarning("Failed to extract claims from token");
                return Models.AuthenticationState.Unauthenticated();
            }

            return Models.AuthenticationState.Authenticated(claimsPrincipal, token.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return Models.AuthenticationState.Unauthenticated();
        }
    }

    /// <inheritdoc/>
    public ClaimsPrincipal? ExtractClaimsFromToken(string accessToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            if (!_jwtHandler.CanReadToken(accessToken))
            {
                _logger.LogWarning("Invalid JWT token format");
                return null;
            }

            var jwtToken = _jwtHandler.ReadJwtToken(accessToken);
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");

            return new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting claims from token");
            return null;
        }
    }
}
