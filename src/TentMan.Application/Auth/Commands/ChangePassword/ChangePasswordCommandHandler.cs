using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Handler for password change command.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IAuthenticationService authenticationService,
        ICurrentUser currentUser,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Use current user if not explicitly provided
        var userId = !string.IsNullOrWhiteSpace(request.UserId)
            ? request.UserId
            : _currentUser.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Password change attempt without valid user ID");
            return Result.Failure("User not authenticated");
        }

        _logger.LogInformation("Password change attempt for user: {UserId}", userId);

        var result = await _authenticationService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Password change failed for user: {UserId}. Error: {Error}",
                userId,
                result.Error);
            
            return result;
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

        return result;
    }
}
