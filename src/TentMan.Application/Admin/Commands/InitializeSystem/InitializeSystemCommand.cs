using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Admin.Commands.InitializeSystem;

/// <summary>
/// Command to initialize the system with roles and a super admin user.
/// This should only be executed once during initial system setup.
/// </summary>
public record InitializeSystemCommand(
    string UserName,
    string Email,
    string Password
) : IRequest<Result<InitializationResult>>;

/// <summary>
/// Result of system initialization.
/// </summary>
public record InitializationResult(
    bool RolesCreated,
    int RolesCount,
    bool UserCreated,
    Guid? UserId,
    string Message
);
