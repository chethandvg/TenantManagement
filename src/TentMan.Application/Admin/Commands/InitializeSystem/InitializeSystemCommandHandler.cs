using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using TentMan.Domain.Constants;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Admin.Commands.InitializeSystem;

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

            // Validate that if Owner is provided, Organization must also be provided
            if (request.Owner != null && request.Organization == null)
            {
                _logger.LogWarning("Owner details provided without organization details");
                return Result<InitializationResult>.Failure(
                    "Organization details must be provided when creating an owner.");
            }

            // ✅ FIX: Use ExecuteWithRetryAsync to handle retries with transactions
            // This resolves the conflict between SqlServerRetryingExecutionStrategy and user-initiated transactions
            var (rolesCreated, rolesCount, userId, orgId, ownerId) = await _unitOfWork.ExecuteWithRetryAsync(async () =>
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

                    // Step 4: Create organization if provided
                    Guid? orgId = null;
                    if (request.Organization != null)
                    {
                        orgId = await CreateOrganizationAsync(request.Organization, cancellationToken);
                    }

                    // Step 5: Create owner if provided
                    Guid? ownerId = null;
                    if (request.Owner != null && orgId.HasValue)
                    {
                        ownerId = await CreateOwnerAsync(request.Owner, orgId.Value, userId, cancellationToken);
                    }

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    return (created, count, userId, orgId, ownerId);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }, cancellationToken);

            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.Append($"System initialized successfully. Created {rolesCount} roles and super admin user.");
            
            if (orgId.HasValue)
            {
                messageBuilder.Append($" Organization created with ID {orgId}.");
            }
            
            if (ownerId.HasValue)
            {
                messageBuilder.Append($" Owner created and linked to superadmin.");
            }

            _logger.LogInformation(
                "System initialized successfully. Created {RolesCount} roles, super admin user {Email}, Organization: {OrgCreated}, Owner: {OwnerCreated}",
                rolesCount,
                request.Email,
                orgId.HasValue,
                ownerId.HasValue);

            var result = new InitializationResult(
                RolesCreated: rolesCreated,
                RolesCount: rolesCount,
                UserCreated: true,
                UserId: userId,
                OrganizationCreated: orgId.HasValue,
                OrganizationId: orgId,
                OwnerCreated: ownerId.HasValue,
                OwnerId: ownerId,
                Message: messageBuilder.ToString()
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

    private async Task<Guid> CreateOrganizationAsync(
        OrganizationData organizationData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating organization: {OrgName}", organizationData.Name);

        var organization = new Organization
        {
            Name = organizationData.Name,
            TimeZone = organizationData.TimeZone ?? "Asia/Kolkata", // Use provided or default
            IsActive = true,
            CreatedAtUtc = _timeProvider.UtcNow,
            CreatedBy = "System"
        };

        var createdOrg = await _unitOfWork.Organizations.AddAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Organization created with ID: {OrgId}", createdOrg.Id);

        return createdOrg.Id;
    }

    private async Task<Guid> CreateOwnerAsync(
        OwnerData ownerData,
        Guid organizationId,
        Guid linkedUserId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating owner: {DisplayName} for organization {OrgId}", ownerData.DisplayName, organizationId);

        var owner = new Owner
        {
            OrgId = organizationId,
            OwnerType = ownerData.OwnerType,
            DisplayName = ownerData.DisplayName,
            Phone = ownerData.Phone,
            Email = ownerData.Email,
            Pan = ownerData.Pan,
            Gstin = ownerData.Gstin,
            LinkedUserId = linkedUserId, // Link to the superadmin user
            CreatedAtUtc = _timeProvider.UtcNow,
            CreatedBy = "System"
        };

        var createdOwner = await _unitOfWork.Owners.AddAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Owner created with ID: {OwnerId}, linked to user {UserId}", createdOwner.Id, linkedUserId);

        return createdOwner.Id;
    }
}
