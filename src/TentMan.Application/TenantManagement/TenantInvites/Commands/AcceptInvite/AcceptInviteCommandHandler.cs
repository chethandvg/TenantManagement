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

        // Validate email matches invite (if invite has email)
        if (!string.IsNullOrWhiteSpace(invite.Email) && 
            !string.Equals(invite.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email does not match the invite");
        }

        // Check if user with email already exists
        var existingUserByEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUserByEmail != null)
        {
            throw new InvalidOperationException("Unable to create account. Please contact support.");
        }

        // Check if user with username already exists
        var existingUserByUsername = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);
        if (existingUserByUsername != null)
        {
            throw new InvalidOperationException("Username is already taken");
        }

        // Use optimistic concurrency to prevent race condition on invite usage
        try
        {
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

            // Mark invite as used (with original row version for concurrency control)
            invite.IsUsed = true;
            invite.UsedAtUtc = DateTime.UtcNow;
            invite.AcceptedByUserId = user.Id;
            await _inviteRepository.UpdateAsync(invite, invite.RowVersion, cancellationToken);

            // Link user to tenant
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(invite.TenantId, cancellationToken);
            if (tenant != null)
            {
                tenant.LinkedUserId = user.Id;
                await _unitOfWork.Tenants.UpdateAsync(tenant, tenant.RowVersion, cancellationToken);
            }

            // Assign Tenant role to the user
            var tenantRole = await _unitOfWork.Roles.GetByNameAsync(ApplicationRoles.Tenant, cancellationToken);
            if (tenantRole != null)
            {
                var userRole = new Domain.Entities.Identity.UserRole
                {
                    UserId = user.Id,
                    RoleId = tenantRole.Id
                };
                await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            Logger.LogInformation("Invite accepted and user created: {UserId}, linked to tenant: {TenantId}", user.Id, invite.TenantId);

            // Generate JWT tokens
            var roles = tenantRole != null ? new List<string> { ApplicationRoles.Tenant } : new List<string>();
            var accessToken = _jwtTokenService.GenerateAccessToken(
                user.Id.ToString(),
                user.Email,
                user.UserName,
                roles
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
                    Roles = roles
                }
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            Logger.LogWarning("Concurrency conflict when accepting invite - invite may have been used by another request");
            throw new InvalidOperationException("Invite has already been used");
        }
    }
}
