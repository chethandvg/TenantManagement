using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Auth.Commands.Register;

/// <summary>
/// Handler for user registration command.
/// </summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthenticationResult>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<RegisterCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResult>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user with email: {Email}", request.Email);

        // Register the user through authentication service
        var result = await _authenticationService.RegisterAsync(
            request.Email,
            request.Password,
            request.UserName,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Registration failed for email: {Email}. Error: {Error}", 
                request.Email, 
                result.Error);
            
            return result;
        }

        _logger.LogInformation(
            "User registered successfully with email: {Email}, UserId: {UserId}",
            request.Email,
            result.Value!.User.Id);

        return result;
    }
}
