using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using Archu.Domain.Constants;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.InitializeSystem;

/// <summary>
/// Handles system initialization by creating system roles and a super admin user.
/// </summary>
public class InitializeSystemCommandHandler : IRequestHandler<InitializeSystemCommand, Result<InitializationResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<InitializeSystemCommandHandler> _logger;

    public InitializeSystemCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITimeProvider timeProvider,
        ILogger<InitializeSystemCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<InitializationResult>> Handle(
        InitializeSystemCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting system initialization...");

        try
        {
            // Check if system is already initialized (if any users exist)
            var userCount = await _unitOfWork.Users.GetCountAsync(cancellationToken);
            if (userCount > 0)
            {
                _logger.LogWarning("System initialization attempted but users already exist");
                return Result<InitializationResult>.Failure(
                    "System is already initialized. Users already exist in the database.");
            }

            // ✅ FIX: Use ExecuteWithRetryAsync to handle retries with transactions
            // This resolves the conflict between SqlServerRetryingExecutionStrategy and user-initiated transactions
            var (rolesCreated, rolesCount, userId) = await _unitOfWork.ExecuteWithRetryAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Step 1: Create all system roles
                    var (created, count) = await CreateSystemRolesAsync(cancellationToken);

                    // Step 2: Create super admin user
                    var userId = await CreateSuperAdminUserAsync(request, cancellationToken);

                    // Step 3: Assign SuperAdmin role to the user
                    await AssignSuperAdminRoleAsync(userId, cancellationToken);

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    return (created, count, userId);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }, cancellationToken);

            _logger.LogInformation(
                "System initialized successfully. Created {RolesCount} roles and super admin user {Email}",
                rolesCount,
                request.Email);

            var result = new InitializationResult(
                RolesCreated: rolesCreated,
                RolesCount: rolesCount,
                UserCreated: true,
                UserId: userId,
                Message: $"System initialized successfully. Created {rolesCount} roles and super admin user."
            );

            return Result<InitializationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during system initialization");
            return Result<InitializationResult>.Failure($"System initialization failed: {ex.Message}");
        }
    }

    private async Task<(bool created, int count)> CreateSystemRolesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating system roles...");

        var rolesToCreate = new[]
        {
            new { Name = RoleNames.Guest, Description = "Guest role with minimal read-only access" },
            new { Name = RoleNames.User, Description = "Standard user role with basic application access" },
            new { Name = RoleNames.Manager, Description = "Manager role with elevated permissions for team management" },
            new { Name = RoleNames.Administrator, Description = "Administrator role with full system access" },
            new { Name = RoleNames.SuperAdmin, Description = "System administrator with unrestricted access" }
        };

        var existingRoles = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
        var existingRoleNames = existingRoles.Select(r => r.NormalizedName).ToHashSet();

        var newRolesCount = 0;

        foreach (var roleInfo in rolesToCreate)
        {
            var normalizedName = roleInfo.Name.ToUpperInvariant();

            if (existingRoleNames.Contains(normalizedName))
            {
                _logger.LogDebug("Role '{RoleName}' already exists, skipping", roleInfo.Name);
                continue;
            }

            var role = new ApplicationRole
            {
                Name = roleInfo.Name,
                NormalizedName = normalizedName,
                Description = roleInfo.Description
            };

            await _unitOfWork.Roles.AddAsync(role, cancellationToken);
            newRolesCount++;

            _logger.LogInformation("Created role: {RoleName}", roleInfo.Name);
        }

        if (newRolesCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created {Count} system roles", newRolesCount);
        }

        return (newRolesCount > 0, newRolesCount);
    }

    private async Task<Guid> CreateSuperAdminUserAsync(
        InitializeSystemCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating super admin user: {UserName}", request.UserName);

        // Double-check username doesn't exist
        if (await _unitOfWork.Users.UserNameExistsAsync(request.UserName, null, cancellationToken))
        {
            throw new InvalidOperationException($"Username is already taken");
        }

        // Double-check email doesn't exist
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, null, cancellationToken))
        {
            throw new InvalidOperationException($"Email is already registered");
        }

        var superAdminUser = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            EmailConfirmed = true, // Auto-confirm super admin email
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = false, // Super admin should not be locked out
            TwoFactorEnabled = false,
            PhoneNumberConfirmed = false
        };

        var createdUser = await _unitOfWork.Users.AddAsync(superAdminUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Super admin user created with ID: {UserId}", createdUser.Id);

        return createdUser.Id;
    }

    private async Task AssignSuperAdminRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning SuperAdmin role to user {UserId}", userId);

        // Get SuperAdmin role
        var superAdminRole = await _unitOfWork.Roles.GetByNameAsync(RoleNames.SuperAdmin, cancellationToken);
        if (superAdminRole == null)
        {
            throw new InvalidOperationException("SuperAdmin role not found. Ensure roles are created first.");
        }

        // Check if user already has this role
        var hasRole = await _unitOfWork.UserRoles.UserHasRoleAsync(userId, superAdminRole.Id, cancellationToken);
        if (hasRole)
        {
            _logger.LogDebug("User already has SuperAdmin role, skipping assignment");
            return;
        }

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = superAdminRole.Id,
            AssignedAtUtc = _timeProvider.UtcNow,
            AssignedBy = "System"
        };

        await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SuperAdmin role assigned successfully");
    }
}
