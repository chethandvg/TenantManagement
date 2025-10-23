using System.Text.Json;
using Archu.ApiClient.Authentication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Archu.ApiClient.Authentication.Storage;

/// <summary>
/// Browser local storage implementation for token storage (suitable for Blazor WebAssembly).
/// Uses JavaScript interop to store tokens in browser's localStorage.
/// </summary>
public sealed class BrowserLocalTokenStorage : ITokenStorage
{
    private const string StorageKey = "archu_auth_token";
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<BrowserLocalTokenStorage> _logger;

    public BrowserLocalTokenStorage(
        IJSRuntime jsRuntime,
        ILogger<BrowserLocalTokenStorage> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StoreTokenAsync(StoredToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            _logger.LogDebug("Token stored in browser local storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store token in browser local storage");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<StoredToken?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogDebug("No token found in browser local storage");
                return null;
            }

            var token = JsonSerializer.Deserialize<StoredToken>(json);
            _logger.LogDebug("Token retrieved from browser local storage");
            return token;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize token from browser local storage");
            // Remove corrupted token
            await RemoveTokenAsync(cancellationToken);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve token from browser local storage");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            _logger.LogDebug("Token removed from browser local storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove token from browser local storage");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(cancellationToken);
            return token != null && !token.IsExpired();
        }
        catch
        {
            return false;
        }
    }
}
