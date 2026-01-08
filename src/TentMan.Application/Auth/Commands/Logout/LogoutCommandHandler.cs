using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Auth.Commands.Logout;

/// <summary>
/// Handler for user logout command.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IAuthenticationService authenticationService,
        ICurrentUser currentUser,
        ILogger<LogoutCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Use current user if not explicitly provided
        var userId = !string.IsNullOrWhiteSpace(request.UserId) 
            ? request.UserId 
            : _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Logout attempt without valid user ID");
            return Result.Failure("User not authenticated");
        }

        _logger.LogInformation("Logging out user: {UserId}", userId);

        var result = await _authenticationService.LogoutAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Logout failed for user: {UserId}. Error: {Error}", 
                userId, 
                result.Error);
            
            return result;
        }

        _logger.LogInformation("User logged out successfully: {UserId}", userId);

        return result;
    }
}
