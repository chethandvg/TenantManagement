using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
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

            _logger.LogInformation("User {Email} registered successfully", email);

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

        var user = await _unitOfWork.Users.GetByIdAsync(userGuid, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Email confirmation failed: User {UserId} not found", userId);
            return Result.Failure("User not found");
        }

        // Simple token validation (you might want to implement proper token generation/validation)
        if (user.SecurityStamp != confirmationToken)
        {
            _logger.LogWarning("Email confirmation failed: Invalid token for user {UserId}", userId);
            return Result.Failure("Invalid or expired confirmation token");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email already confirmed for user {UserId}", userId);
            return Result.Success();
        }

        user.EmailConfirmed = true;

        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
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

        // Generate password reset token (SecurityStamp)
        user.SecurityStamp = Guid.NewGuid().ToString();

        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token generated for user {UserId}", user.Id);

        // TODO: Send email with reset token
        // In a real implementation, you would send an email here
        // For now, we just log it (in production, remove this log)
        _logger.LogDebug("Password reset token for {Email}: {Token}", email, user.SecurityStamp);

        return Result.Success();
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

        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset failed: User with email {Email} not found", email);
            return Result.Failure("Invalid email or reset token");
        }

        // Validate reset token
        if (user.SecurityStamp != resetToken)
        {
            _logger.LogWarning("Password reset failed: Invalid token for user {Email}", email);
            return Result.Failure("Invalid or expired reset token");
        }

        // Update password and security stamp
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate the reset token
        user.AccessFailedCount = 0; // Reset failed login attempts
        user.LockoutEnd = null; // Remove any lockout

        await _unitOfWork.Users.UpdateAsync(user, user.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for user {Email}", email);

        return Result.Success();
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
        // Load user roles
        var userRoles = user.UserRoles?.Select(ur => ur.Role?.Name ?? string.Empty).Where(r => !string.IsNullOrEmpty(r)).ToList()
            ?? new List<string>();

        // Generate JWT access token
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id.ToString(),
            user.Email,
            user.UserName,
            userRoles);

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
