using Archu.ApiClient.Authentication.Models;
using Archu.ApiClient.Authentication.Providers;
using Archu.ApiClient.Exceptions;
using Archu.ApiClient.Services;
using Archu.Contracts.Authentication;
using Microsoft.Extensions.Logging;

namespace Archu.ApiClient.Authentication.Services;

/// <summary>
/// Interface for authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<AuthenticationResult> RegisterAsync(string email, string password, string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the authentication token.
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    Task<AuthenticationState> GetAuthenticationStateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of an authentication operation.
/// </summary>
public sealed record AuthenticationResult
{
    /// <summary>
    /// Gets a value indicating whether the authentication was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the authentication state.
    /// </summary>
    public AuthenticationState? AuthenticationState { get; init; }

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    public static AuthenticationResult Succeeded(AuthenticationState authState) => new()
    {
        Success = true,
        AuthenticationState = authState
    };

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    public static AuthenticationResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Default implementation of authentication service.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ITokenManager _tokenManager;
    private readonly IAuthenticationApiClient _authApiClient;
    private readonly ApiAuthenticationStateProvider? _authStateProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ITokenManager tokenManager,
        IAuthenticationApiClient authApiClient,
        ILogger<AuthenticationService> logger,
        ApiAuthenticationStateProvider? authStateProvider = null)
    {
        _tokenManager = tokenManager;
        _authApiClient = authApiClient;
        _logger = logger;
        _authStateProvider = authStateProvider;
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to authenticate user: {Email}", email);

            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var response = await _authApiClient.LoginAsync(loginRequest, cancellationToken);

            if (!response.Success || response.Data == null)
            {
                var errorMessage = response.Message ?? "Login failed";
                _logger.LogWarning("Authentication failed for user: {Email} - {Error}", email, errorMessage);
                return AuthenticationResult.Failed(errorMessage);
            }

            var authResponse = response.Data;

            // Store the tokens
            var tokenResponse = new TokenResponse
            {
                AccessToken = authResponse.AccessToken,
                RefreshToken = authResponse.RefreshToken,
                ExpiresIn = authResponse.ExpiresIn
            };

            await _tokenManager.StoreTokenAsync(tokenResponse, cancellationToken);

            // Get authentication state from stored token
            var authState = await _tokenManager.GetAuthenticationStateAsync(cancellationToken);

            // Notify the authentication state provider
            if (_authStateProvider != null)
            {
                await _authStateProvider.MarkUserAsAuthenticatedAsync(authState.UserName ?? email);
            }

            _logger.LogInformation("User authenticated successfully: {Email}", email);
            return AuthenticationResult.Succeeded(authState);
        }
        catch (AuthorizationException ex)
        {
            _logger.LogWarning(ex, "Authentication failed for user: {Email}", email);
            return AuthenticationResult.Failed("Invalid email or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user: {Email}", email);
            return AuthenticationResult.Failed("An error occurred during authentication");
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> RegisterAsync(
        string email,
        string password,
        string userName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to register user: {Email}", email);

            var registerRequest = new RegisterRequest
            {
                Email = email,
                Password = password,
                UserName = userName
            };

            var response = await _authApiClient.RegisterAsync(registerRequest, cancellationToken);

            if (!response.Success || response.Data == null)
            {
                var errorMessage = response.Message ?? "Registration failed";
                _logger.LogWarning("Registration failed for user: {Email} - {Error}", email, errorMessage);
                return AuthenticationResult.Failed(errorMessage);
            }

            var authResponse = response.Data;

            // Store the tokens
            var tokenResponse = new TokenResponse
            {
                AccessToken = authResponse.AccessToken,
                RefreshToken = authResponse.RefreshToken,
                ExpiresIn = authResponse.ExpiresIn
            };

            await _tokenManager.StoreTokenAsync(tokenResponse, cancellationToken);

            // Get authentication state from stored token
            var authState = await _tokenManager.GetAuthenticationStateAsync(cancellationToken);

            // Notify the authentication state provider
            if (_authStateProvider != null)
            {
                await _authStateProvider.MarkUserAsAuthenticatedAsync(authState.UserName ?? email);
            }

            _logger.LogInformation("User registered successfully: {Email}", email);
            return AuthenticationResult.Succeeded(authState);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Registration validation failed for user: {Email}", email);
            var errorMessage = string.Join(", ", ex.Errors);
            return AuthenticationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user: {Email}", email);
            return AuthenticationResult.Failed("An error occurred during registration");
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Logging out user");

            // Call logout endpoint (will fail if not authenticated, but that's okay)
            try
            {
                await _authApiClient.LogoutAsync(cancellationToken);
            }
            catch (AuthorizationException)
            {
                // User not authenticated on server, but we still want to clear local tokens
                _logger.LogDebug("Logout API call failed (user not authenticated), clearing local tokens");
            }

            // Remove local tokens
            await _tokenManager.RemoveTokenAsync(cancellationToken);

            // Notify the authentication state provider
            if (_authStateProvider != null)
            {
                await _authStateProvider.MarkUserAsLoggedOutAsync();
            }

            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to refresh authentication token");

            // Get current token with refresh token
            var currentToken = await _tokenManager.GetStoredTokenAsync(cancellationToken);
            if (currentToken?.RefreshToken == null)
            {
                _logger.LogWarning("No refresh token available");
                return AuthenticationResult.Failed("No refresh token available");
            }

            var refreshRequest = new RefreshTokenRequest
            {
                RefreshToken = currentToken.RefreshToken
            };

            var response = await _authApiClient.RefreshTokenAsync(refreshRequest, cancellationToken);

            if (!response.Success || response.Data == null)
            {
                var errorMessage = response.Message ?? "Token refresh failed";
                _logger.LogWarning("Token refresh failed: {Error}", errorMessage);
                return AuthenticationResult.Failed(errorMessage);
            }

            var authResponse = response.Data;

            // Store the new tokens
            var tokenResponse = new TokenResponse
            {
                AccessToken = authResponse.AccessToken,
                RefreshToken = authResponse.RefreshToken,
                ExpiresIn = authResponse.ExpiresIn
            };

            await _tokenManager.StoreTokenAsync(tokenResponse, cancellationToken);

            // Get authentication state from stored token
            var authState = await _tokenManager.GetAuthenticationStateAsync(cancellationToken);

            // Notify the authentication state provider
            _authStateProvider?.NotifyAuthenticationStateChanged();

            _logger.LogInformation("Token refreshed successfully");
            return AuthenticationResult.Succeeded(authState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return AuthenticationResult.Failed("Failed to refresh token");
        }
    }

    /// <inheritdoc/>
    public Task<AuthenticationState> GetAuthenticationStateAsync(CancellationToken cancellationToken = default)
    {
        return _tokenManager.GetAuthenticationStateAsync(cancellationToken);
    }
}
