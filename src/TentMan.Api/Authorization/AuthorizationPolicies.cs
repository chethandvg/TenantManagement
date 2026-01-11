namespace TentMan.Api.Authorization;

/// <summary>
/// NOTE: Authorization policy names, role names, permission values, and claim types 
/// are now centralized in TentMan.Shared.Constants.Authorization to avoid duplication
/// and ensure consistency across all application layers.
/// 
/// This file is intentionally left minimal and may be removed in future refactoring.
/// All authorization constants should be referenced from:
/// - TentMan.Shared.Constants.Authorization.PolicyNames
/// - TentMan.Shared.Constants.Authorization.RoleNames  
/// - TentMan.Shared.Constants.Authorization.PermissionValues
/// - TentMan.Shared.Constants.Authorization.ClaimTypes
/// </summary>
[Obsolete("Use TentMan.Shared.Constants.Authorization instead. This class is kept temporarily for backwards compatibility.", false)]
public static class AuthorizationPolicies
{
    // This class intentionally left empty.
    // All constants have been moved to TentMan.Shared.Constants.Authorization
}

[Obsolete("Use TentMan.Shared.Constants.Authorization.RoleNames instead.", false)]
public static class Roles
{
    // This class intentionally left empty.
    // All constants have been moved to TentMan.Shared.Constants.Authorization.RoleNames
}

[Obsolete("Use TentMan.Shared.Constants.Authorization.PermissionValues instead.", false)]
public static class Permissions
{
    // This class intentionally left empty.
    // All constants have been moved to TentMan.Shared.Constants.Authorization.PermissionValues
}

[Obsolete("Use TentMan.Shared.Constants.Authorization.ClaimTypes instead.", false)]
public static class CustomClaimTypes
{
    // This class intentionally left empty.
    // All constants have been moved to TentMan.Shared.Constants.Authorization.ClaimTypes
}
