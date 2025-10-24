using System.Net.Http.Headers;
using Archu.ApiClient.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace Archu.ApiClient.Authentication.Handlers;

/// <summary>
/// HTTP message handler that automatically attaches authentication tokens to outgoing requests.
/// Intelligently skips token attachment for unauthenticated authentication endpoints.
/// </summary>
public sealed class AuthenticationMessageHandler : DelegatingHandler
{
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<AuthenticationMessageHandler> _logger;

    // Endpoints that should NOT have authentication tokens attached
    private static readonly HashSet<string> UnauthenticatedEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/register",
        "/login",
        "/refresh-token",
        "/forgot-password",
        "/reset-password",
        "/confirm-email"
    };

    public AuthenticationMessageHandler(
        ITokenManager tokenManager,
        ILogger<AuthenticationMessageHandler> logger)
    {
        _tokenManager = tokenManager;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Skip if authorization header is already set
        if (request.Headers.Authorization != null)
        {
            _logger.LogDebug("Authorization header already present, skipping token attachment");
            return await base.SendAsync(request, cancellationToken);
        }

        // Check if this is an unauthenticated authentication endpoint
        if (IsUnauthenticatedEndpoint(request.RequestUri))
        {
            _logger.LogDebug("Skipping token attachment for unauthenticated endpoint: {Method} {Uri}",
                request.Method,
                request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            var accessToken = await _tokenManager.GetAccessTokenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _logger.LogDebug("Bearer token attached to request: {Method} {Uri}",
                    request.Method,
                    request.RequestUri);
            }
            else
            {
                _logger.LogDebug("No valid token available for request: {Method} {Uri}",
                    request.Method,
                    request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching authentication token to request");
            // Continue with the request even if token attachment fails
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - token might be expired
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized response. Token may be expired or invalid.");
            // TODO: Implement token refresh logic here
        }

        return response;
    }

    /// <summary>
    /// Determines if the request URI is for an unauthenticated authentication endpoint.
    /// </summary>
    private static bool IsUnauthenticatedEndpoint(Uri? requestUri)
    {
        if (requestUri == null)
        {
            return false;
        }

        var path = requestUri.AbsolutePath;

        // Check if the path ends with any of the unauthenticated endpoints
        return UnauthenticatedEndpoints.Any(endpoint =>
            path.EndsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}
