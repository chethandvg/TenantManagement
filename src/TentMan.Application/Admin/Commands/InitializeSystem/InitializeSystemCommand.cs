using TentMan.Application.Common;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.Admin.Commands.InitializeSystem;

/// <summary>
/// Command to initialize the system with roles and a super admin user.
/// This should only be executed once during initial system setup.
/// </summary>
public record InitializeSystemCommand(
    string UserName,
    string Email,
    string Password,
    OrganizationData? Organization,
    OwnerData? Owner
) : IRequest<Result<InitializationResult>>;

/// <summary>
/// Organization data for initialization.
/// </summary>
public record OrganizationData(
    string Name,
    string? TimeZone
);

/// <summary>
/// Owner data for initialization.
/// </summary>
public record OwnerData(
    OwnerType OwnerType,
    string DisplayName,
    string Phone,
    string Email,
    string? Pan,
    string? Gstin
);

/// <summary>
/// Result of system initialization.
/// </summary>
public record InitializationResult(
    bool RolesCreated,
    int RolesCount,
    bool UserCreated,
    Guid? UserId,
    bool OrganizationCreated,
    Guid? OrganizationId,
    bool OwnerCreated,
    Guid? OwnerId,
    string Message
);
