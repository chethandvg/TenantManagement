using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using Archu.Domain.Constants;
using Archu.Domain.Entities.Identity;
using Microsoft.Extensions.Logging;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Implementation of authentication service using JWT tokens.
/// Handles user registration, login, token refresh, and password management.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITimeProvider timeProvider,
        ILogger<AuthenticationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<AuthenticationResult>> RegisterAsync(
        string email,
        string password,
        string userName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering new user with email: {Email}", email);

        // Validate input
        if (string.IsNullOrWhiteSpace(email))
            return Result<AuthenticationResult>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(password))
            return Result<AuthenticationResult>.Failure("Password is required");

        if (string.IsNullOrWhiteSpace(userName))
            return Result<AuthenticationResult>.Failure("Username is required");

        // Check if email already exists
        if (await _unitOfWork.Users.EmailExistsAsync(email, null, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", email);
            return Result<AuthenticationResult>.Failure("Email is already in use");
        }

        // Check if username already exists
        if (await _unitOfWork.Users.UserNameExistsAsync(userName, null, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Username {UserName} already exists", userName);
            return Result<AuthenticationResult>.Failure("Username is already in use");
        }

        // Create new user
        var user = new ApplicationUser
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = userName,
            PasswordHash = _passwordHasher.HashPassword(password),
            EmailConfirmed = false, // Require email confirmation
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            AccessFailedCount = 0,
            TwoFactorEnabled = false
        };

        try
        {
            // Add user to database
            await _unitOfWork.Users.AddAsync(user, cancellationToken);

            // Assign default "User" role
            var defaultRole = await _unitOfWork.Roles.GetByNameAsync("User", cancellationToken);
            if (defaultRole != null)
            {
                await _unitOfWork.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = defaultRole.Id
                }, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Default 'User' role not found. User registered without role.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ✅ FIX #4: Generate secure email confirmation token
            // This token is time-limited (24 hours) and single-use
            var confirmationToken = await _unitOfWork.EmailConfirmationTokens.CreateTokenAsync(
                user.Id,
                cancellationToken);

            _logger.LogInformation(
                "User {Email} registered successfully with confirmation token expiring at {ExpiresAt}",
                email,
                confirmationToken.ExpiresAtUtc);

            // TODO: Send confirmation email with token
            // In production, integrate email service to send:
            // await _emailService.SendEmailConfirmationAsync(user.Email, confirmationToken.Token);
            // For now, log for development (remove in production)
            _logger.LogDebug(
                "Email confirmation token for {Email}: {Token} (Remove this log in production!)",
                email,
                confirmationToken.Token);

            // Generate authentication result
            return await GenerateAuthenticationResultAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", email);
            return Result<AuthenticationResult>.Failure("An error occurred during registration");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<AuthenticationResult>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", email);

        // Validate input
        if (string.IsNullOrWhiteSpace(email))
            return Result<AuthenticationResult>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(password))
            return Result<AuthenticationResult>.Failure("Password is required");

        // Find user by email
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found", email);
            return Result<AuthenticationResult>.Failure("Invalid email or password");
        }

        // Check if user is locked out
        if (user.IsLockedOut)
        {
            _logger.LogWarning("Login failed: User {Email} is locked out until {LockoutEnd}", email, user.LockoutEnd);
            return Result<AuthenticationResult>.Failure($"Account is locked. Try again after {user.LockoutEnd:g}");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {Email}", email);

            // Increment failed access count
            user.AccessFailedCount++;

            // Lock account after 5 failed attempts
            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = _timeProvider.UtcNow.AddMinutes(15);
                _logger.LogWarning("User {Email} locked out due to failed login attempts", email);
            }

            await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AuthenticationResult>.Failure("Invalid email or password");
        }

        // Reset failed access count on successful login
        if (user.AccessFailedCount > 0)
        {
            user.AccessFailedCount = 0;
            await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("User {Email} logged in successfully", email);

        // Generate authentication result
        return await GenerateAuthenticationResultAsync(user, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<AuthenticationResult>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refresh token request received");

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<AuthenticationResult>.Failure("Refresh token is required");

        // Find user by refresh token
        var user = await _unitOfWork.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Refresh token not found");
            return Result<AuthenticationResult>.Failure("Invalid refresh token");
        }

        // Check if refresh token is expired
        if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= _timeProvider.UtcNow)
        {
            _logger.LogWarning("Refresh token expired for user {UserId}", user.Id);
            return Result<AuthenticationResult>.Failure("Refresh token has expired. Please login again.");
        }

        _logger.LogInformation("Refresh token validated for user {UserId}", user.Id);

        // Generate new authentication result (with new refresh token)
        return await GenerateAuthenticationResultAsync(user, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout request for user: {UserId}", userId);

        if (!Guid.TryParse(userId, out var userGuid))
            return Result.Failure("Invalid user ID");

        var user = await _unitOfWork.Users.GetByIdAsync(userGuid, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Logout failed: User {UserId} not found", userId);
            return Result.Failure("User not found");
        }

        // Revoke refresh token
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged out successfully", userId);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> ConfirmEmailAsync(
        string userId,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email confirmation attempt for user: {UserId}", userId);

        if (!Guid.TryParse(userId, out var userGuid))
            return Result.Failure("Invalid user ID");

        if (string.IsNullOrWhiteSpace(confirmationToken))
            return Result.Failure("Confirmation token is required");

        // ✅ FIX #4 IMPLEMENTED: Secure email confirmation using dedicated token repository
        // 1. Get valid token from repository (checks expiry, used status, revoked status)
        var token = await _unitOfWork.EmailConfirmationTokens.GetValidTokenAsync(confirmationToken, cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Email confirmation failed: Invalid or expired token");
            return Result.Failure("Invalid or expired confirmation token");
        }

        // 2. Validate token belongs to the correct user
        if (token.UserId != userGuid)
        {
            _logger.LogWarning(
                "Email confirmation failed: Token user mismatch. Expected {ExpectedUserId}, got {TokenUserId}",
                userGuid,
                token.UserId);
            return Result.Failure("Invalid confirmation token");
        }

        // 3. Get the user
        var user = await _unitOfWork.Users.GetByIdAsync(userGuid, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Email confirmation failed: User {UserId} not found", userId);
            return Result.Failure("User not found");
        }

        // 4. Check if email already confirmed
        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email already confirmed for user {UserId}", userId);
            // Still mark token as used even if email was already confirmed
            await _unitOfWork.EmailConfirmationTokens.MarkAsUsedAsync(token, cancellationToken);
            return Result.Success();
        }

        // 5. Confirm email
        user.EmailConfirmed = true;
        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);

        // 6. Mark token as used (prevents reuse)
        await _unitOfWork.EmailConfirmationTokens.MarkAsUsedAsync(token, cancellationToken);

        // 7. Revoke any other outstanding confirmation tokens for this user
        await _unitOfWork.EmailConfirmationTokens.RevokeAllForUserAsync(userGuid, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email confirmed successfully for user {UserId}", userId);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", email);

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure("Email is required");

        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

        // Always return success to prevent email enumeration
        if (user == null)
        {
            _logger.LogDebug("Password reset requested for non-existent email: {Email}", email);
            return Result.Success(); // Return success to prevent email enumeration
        }

        try
        {
            // ✅ FIX #1 IMPLEMENTED: Secure password reset using dedicated token repository
            // 1. Revoke all existing password reset tokens for user (invalidate old tokens)
            await _unitOfWork.PasswordResetTokens.RevokeAllForUserAsync(user.Id, cancellationToken);

            // 2. Create new time-limited token (1 hour expiry)
            var resetToken = await _unitOfWork.PasswordResetTokens.CreateTokenAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Password reset token generated for user {UserId}, expires at {ExpiresAt}",
                user.Id,
                resetToken.ExpiresAtUtc);

            // TODO: Send email with reset token using email service
            // In production, integrate email service (SendGrid, AWS SES, etc.)
            // await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken.Token);
            
            // For development only - log token (REMOVE IN PRODUCTION!)
            _logger.LogDebug(
                "Password reset token for {Email}: {Token} (Remove this log in production!)",
                email,
                resetToken.Token);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for user {Email}", email);
            
            // Return success to prevent email enumeration even on errors
            // But log the actual error for debugging
            return Result.Success();
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", email);

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(resetToken))
            return Result.Failure("Reset token is required");

        if (string.IsNullOrWhiteSpace(newPassword))
            return Result.Failure("New password is required");

        // ✅ FIX #1 IMPLEMENTED: Secure password reset validation
        // 1. Get valid token from repository (checks expiry, used status, revoked status)
        var token = await _unitOfWork.PasswordResetTokens.GetValidTokenAsync(resetToken, cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Password reset failed: Invalid or expired token for email {Email}", email);
            return Result.Failure("Invalid or expired reset token");
        }

        // 2. Get the user
        var user = await _unitOfWork.Users.GetByIdAsync(token.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset failed: User {UserId} not found", token.UserId);
            return Result.Failure("Invalid or expired reset token");
        }

        // 3. Validate email matches (additional security check)
        if (!user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Password reset failed: Email mismatch. Token email {TokenEmail}, provided email {ProvidedEmail}",
                user.Email,
                email);
            return Result.Failure("Invalid email or reset token");
        }

        // 4. Check if token is actually valid at current time
        if (!token.IsValid(_timeProvider.UtcNow))
        {
            _logger.LogWarning("Password reset failed: Token expired or invalid for user {UserId}", user.Id);
            return Result.Failure("Invalid or expired reset token");
        }

        try
        {
            // 5. Update password and security stamp
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing JWT tokens
            user.AccessFailedCount = 0; // Reset failed login attempts
            user.LockoutEnd = null; // Remove any lockout

            await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);

            // 6. Mark token as used (prevents reuse)
            await _unitOfWork.PasswordResetTokens.MarkAsUsedAsync(token, cancellationToken);

            // 7. Revoke all other outstanding password reset tokens for this user
            await _unitOfWork.PasswordResetTokens.RevokeAllForUserAsync(user.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successfully for user {Email}", email);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {Email}", email);
            return Result.Failure("An error occurred while resetting password");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Change password request for user: {UserId}", userId);

        if (!Guid.TryParse(userId, out var userGuid))
            return Result.Failure("Invalid user ID");

        if (string.IsNullOrWhiteSpace(currentPassword))
            return Result.Failure("Current password is required");

        if (string.IsNullOrWhiteSpace(newPassword))
            return Result.Failure("New password is required");

        var user = await _unitOfWork.Users.GetByIdAsync(userGuid, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Change password failed: User {UserId} not found", userId);
            return Result.Failure("User not found");
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password failed: Invalid current password for user {UserId}", userId);
            return Result.Failure("Current password is incorrect");
        }

        // Update password and security stamp
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens

        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed successfully for user {UserId}", userId);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<bool>.Success(false);

        var user = await _unitOfWork.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken);

        if (user == null)
            return Result<bool>.Success(false);

        if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= _timeProvider.UtcNow)
            return Result<bool>.Success(false);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Generates authentication result with access token and refresh token.
    /// </summary>
    private async Task<Result<AuthenticationResult>> GenerateAuthenticationResultAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        // ✅ FIX #5: Handle navigation properties correctly
        // User must be loaded with .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        // from repository to ensure navigation properties are available
        // ✅ Workflow comment: capture role metadata so dynamic database assignments can be
        // reconciled with static defaults when partial seeding occurs.
        var userRoleEntries = user.UserRoles?
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => (ur.RoleId, RoleName: ur.Role!.Name!))
            .ToList() ?? new List<(Guid RoleId, string RoleName)>();

        var userRoles = userRoleEntries
            .Select(entry => entry.RoleName)
            .ToList();

        var roleIdToRoleName = userRoleEntries
            .GroupBy(entry => entry.RoleId)
            .ToDictionary(
                group => group.Key,
                group => group.First().RoleName);

        // If no roles loaded, it might mean user was not properly included
        if (user.UserRoles != null && user.UserRoles.Any() && !userRoles.Any())
        {
            _logger.LogWarning(
                "User {UserId} has UserRoles but no Role names loaded. Check repository includes.",
                user.Id);
        }

        // ✅ Workflow comment: enrich the JWT with claim-based state that downstream
        // authorization handlers depend on (permissions, email verification, MFA).
        var additionalClaims = new List<Claim>
        {
            new(CustomClaimTypes.EmailVerified, user.EmailConfirmed.ToString()),
            new(CustomClaimTypes.TwoFactorEnabled, user.TwoFactorEnabled.ToString())
        };

        // ✅ Workflow comment: hydrate the JWT with dynamic permission assignments resolved from the database
        // so that authorization policies respect runtime configuration instead of static lookup tables.
        var roleIds = roleIdToRoleName.Keys.ToArray();

        IReadOnlyCollection<string> rolePermissionNames = Array.Empty<string>();
        HashSet<Guid> rolesWithDatabaseAssignments = new();

        if (roleIds.Length > 0)
        {
            var roleAssignments = await _unitOfWork.RolePermissions
                .GetByRoleIdsAsync(roleIds, cancellationToken);

            rolesWithDatabaseAssignments = roleAssignments
                .Select(rolePermission => rolePermission.RoleId)
                .Where(roleId => roleId != Guid.Empty)
                .ToHashSet();

            rolePermissionNames = await _unitOfWork.RolePermissions
                .GetPermissionNamesByRoleIdsAsync(roleIds, cancellationToken);
        }

        var directPermissionNames = await _unitOfWork.UserPermissions
            .GetPermissionNamesByUserIdAsync(user.Id, cancellationToken);

        var normalizedPermissionNames = new HashSet<string>(StringComparer.Ordinal);
        normalizedPermissionNames.UnionWith(rolePermissionNames);
        normalizedPermissionNames.UnionWith(directPermissionNames);

        var resolvedPermissionValues = new HashSet<string>(StringComparer.Ordinal);

        if (normalizedPermissionNames.Count > 0)
        {
            var permissionEntities = await _unitOfWork.Permissions
                .GetByNormalizedNamesAsync(normalizedPermissionNames, cancellationToken);

            var permissionLookup = permissionEntities
                .ToDictionary(permission => permission.NormalizedName, permission => permission.Name, StringComparer.Ordinal);

            foreach (var normalizedName in normalizedPermissionNames)
            {
                if (permissionLookup.TryGetValue(normalizedName, out var permissionValue))
                {
                    if (!string.IsNullOrWhiteSpace(permissionValue))
                    {
                        resolvedPermissionValues.Add(permissionValue);
                    }
                    else
                    {
                        _logger.LogError(
                            "Permission entity for normalized name '{NormalizedPermission}' was found, but its Name value is null or whitespace. Check permission seeding and data integrity.",
                            normalizedName);

                        throw new InvalidOperationException(
                            $"Permission entity for normalized name '{normalizedName}' has an invalid Name value (null or whitespace).");
                    }
                }
                else
                {
                    _logger.LogError(
                        "Normalized permission '{NormalizedPermission}' was returned from assignments but the permission entity was not found. Ensure permission data is seeded before issuing tokens.",
                        normalizedName);

                    throw new InvalidOperationException(
                        $"Permission with normalized name '{normalizedName}' could not be resolved.");
                }
            }
        }

        // ✅ Workflow comment: fall back to static role claims only for roles lacking database assignments
        // so partially configured tenants retain legacy permissions until seeding is complete.
        foreach (var (roleId, roleName) in roleIdToRoleName)
        {
            if (rolesWithDatabaseAssignments.Contains(roleId))
            {
                continue;
            }

            resolvedPermissionValues.UnionWith(RolePermissionClaims.GetPermissionClaimsForRole(roleName));
        }

        var permissionClaims = resolvedPermissionValues
            .Select(permission => new Claim(CustomClaimTypes.Permission, permission));

        additionalClaims.AddRange(permissionClaims);

        // Generate JWT access token
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id.ToString(),
            user.Email,
            user.UserName,
            userRoles,
            additionalClaims);

        // Generate refresh token
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store refresh token in user record
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = _timeProvider.UtcNow.AddDays(7); // 7 days validity

        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create authentication result
        var result = new AuthenticationResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = _timeProvider.UtcNow.AddHours(1), // Access token expires in 1 hour
            RefreshTokenExpiresAt = user.RefreshTokenExpiryTime.Value,
            TokenType = "Bearer",
            User = new UserInfo
            {
                Id = user.Id.ToString(),
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = userRoles
            }
        };

        return Result<AuthenticationResult>.Success(result);
    }
}
