using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Auth.Commands.Login;

/// <summary>
/// Handler for user login command.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthenticationResult>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<LoginCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        // Authenticate the user
        var result = await _authenticationService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            
            // Return generic error message for security (prevent email enumeration)
            return Result<AuthenticationResult>.Failure("Invalid email or password");
        }

        _logger.LogInformation(
            "User logged in successfully. UserId: {UserId}, Email: {Email}",
            result.Value!.User.Id,
            request.Email);

        return result;
    }
}
