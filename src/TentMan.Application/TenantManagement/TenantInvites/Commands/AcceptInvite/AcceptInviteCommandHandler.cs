using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Common;
using TentMan.Contracts.Authentication;
using TentMan.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.AcceptInvite;

public class AcceptInviteCommandHandler : BaseCommandHandler, IRequestHandler<AcceptInviteCommand, AuthenticationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantInviteRepository _inviteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AcceptInviteCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantInviteRepository inviteRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUser currentUser,
        ILogger<AcceptInviteCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthenticationResponse> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Accepting invite with token: {Token}", request.InviteToken);

        // Validate invite token
        var invite = await _inviteRepository.GetByTokenAsync(request.InviteToken, cancellationToken);
        if (invite == null)
        {
            throw new InvalidOperationException("Invalid invite token");
        }

        if (invite.IsUsed)
        {
            throw new InvalidOperationException("Invite has already been used");
        }

        if (invite.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invite has expired");
        }

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Unable to create account. Please contact support.");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            EmailConfirmed = true,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = false
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // Mark invite as used
        invite.IsUsed = true;
        invite.UsedAtUtc = DateTime.UtcNow;
        invite.AcceptedByUserId = user.Id;
        await _inviteRepository.UpdateAsync(invite, invite.RowVersion, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Invite accepted and user created: {UserId}", user.Id);

        // Generate JWT tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id.ToString(),
            user.Email,
            user.UserName,
            new List<string>()
        );
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresIn = (int)_jwtTokenService.GetAccessTokenExpiration().TotalSeconds;
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = _jwtTokenService.GetRefreshTokenExpiryUtc();
        await _userRepository.UpdateAsync(user, user.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer",
            User = new UserInfoDto
            {
                Id = user.Id.ToString(),
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = new List<string>()
            }
        };
    }
}
