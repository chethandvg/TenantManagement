using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Contracts.Admin;
using TentMan.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Admin.Commands.CreateUser;

/// <summary>
/// Handles the creation of a new user.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        ILogger<CreateUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = _currentUser.UserId;
        _logger.LogInformation("Admin {UserId} creating user: {UserName}", adminUserId, request.UserName);

        // Check if username already exists
        if (await _unitOfWork.Users.UserNameExistsAsync(request.UserName, null, cancellationToken))
        {
            _logger.LogWarning("Username {UserName} already exists", request.UserName);
            throw new InvalidOperationException($"Username '{request.UserName}' is already taken");
        }

        // Check if email already exists
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            _logger.LogWarning("Email {Email} already exists", request.Email);
            throw new InvalidOperationException($"Email '{request.Email}' is already registered");
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            EmailConfirmed = request.EmailConfirmed,
            TwoFactorEnabled = request.TwoFactorEnabled,
            PhoneNumber = request.PhoneNumber,
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        };

        var createdUser = await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created with ID: {UserId}", createdUser.Id);

        return new UserDto
        {
            Id = createdUser.Id,
            UserName = createdUser.UserName,
            Email = createdUser.Email,
            EmailConfirmed = createdUser.EmailConfirmed,
            LockoutEnabled = createdUser.LockoutEnabled,
            LockoutEnd = createdUser.LockoutEnd,
            TwoFactorEnabled = createdUser.TwoFactorEnabled,
            PhoneNumber = createdUser.PhoneNumber,
            PhoneNumberConfirmed = createdUser.PhoneNumberConfirmed,
            CreatedAtUtc = createdUser.CreatedAtUtc,
            Roles = new List<string>(),
            RowVersion = createdUser.RowVersion
        };
    }
}
