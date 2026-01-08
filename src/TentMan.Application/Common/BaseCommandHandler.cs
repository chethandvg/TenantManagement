using TentMan.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Common;

/// <summary>
/// Base class for command handlers that provides common functionality.
/// </summary>
public abstract class BaseCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger _logger;

    protected BaseCommandHandler(ICurrentUser currentUser, ILogger logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current authenticated user's ID as a Guid.
    /// </summary>
    /// <param name="operationName">Optional name of the operation for logging purposes.</param>
    /// <returns>The user ID as a Guid.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated or the user ID is invalid.</exception>
    protected Guid GetCurrentUserId(string? operationName = null)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            var operation = string.IsNullOrEmpty(operationName) ? "this operation" : operationName;
            _logger.LogError("Cannot perform {Operation}: User ID not found or invalid", operation);
            throw new UnauthorizedAccessException($"User must be authenticated to {operation}");
        }

        return userIdGuid;
    }

    /// <summary>
    /// Tries to get the current authenticated user's ID as a Guid.
    /// </summary>
    /// <param name="userIdGuid">When this method returns, contains the user ID if successful; otherwise, Guid.Empty.</param>
    /// <returns>True if the user ID was successfully retrieved; otherwise, false.</returns>
    protected bool TryGetCurrentUserId(out Guid userIdGuid)
    {
        var userId = _currentUser.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            userIdGuid = Guid.Empty;
            return false;
        }

        return Guid.TryParse(userId, out userIdGuid);
    }

    /// <summary>
    /// Gets the current user service.
    /// </summary>
    protected ICurrentUser CurrentUser => _currentUser;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;
}
