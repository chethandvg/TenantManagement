using Archu.ApiClient.Authentication.Models;

namespace Archu.ApiClient.Authentication.Storage;

/// <summary>
/// Interface for storing and retrieving authentication tokens.
/// </summary>
public interface ITokenStorage
{
    /// <summary>
    /// Stores a token.
    /// </summary>
    /// <param name="token">The token to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StoreTokenAsync(StoredToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the stored token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored token, or null if no token is stored.</returns>
    Task<StoredToken?> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RemoveTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is stored.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a token is stored, false otherwise.</returns>
    Task<bool> HasTokenAsync(CancellationToken cancellationToken = default);
}
