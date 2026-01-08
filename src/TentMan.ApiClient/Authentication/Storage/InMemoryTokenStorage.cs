using TentMan.ApiClient.Authentication.Models;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Authentication.Storage;

/// <summary>
/// In-memory token storage implementation (suitable for server-side Blazor or development).
/// Note: Tokens are lost when the application restarts.
/// </summary>
public sealed class InMemoryTokenStorage : ITokenStorage, IDisposable
{
    private StoredToken? _token;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<InMemoryTokenStorage> _logger;

    public InMemoryTokenStorage(ILogger<InMemoryTokenStorage> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task StoreTokenAsync(StoredToken token, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _token = token;
            _logger.LogDebug("Token stored in memory");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<StoredToken?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveTokenAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _token = null;
            _logger.LogDebug("Token removed from memory");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasTokenAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _token != null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _lock?.Dispose();
    }
}
