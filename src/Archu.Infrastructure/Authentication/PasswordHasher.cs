using Archu.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Implementation of password hashing using ASP.NET Core Identity's PasswordHasher.
/// Uses PBKDF2 with HMAC-SHA256 for secure password hashing.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher;
    private readonly ILogger<PasswordHasher> _logger;

    public PasswordHasher(ILogger<PasswordHasher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hasher = new PasswordHasher<object>();
    }

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError("Attempted to hash null or empty password");
            throw new ArgumentException("Password cannot be null or whitespace", nameof(password));
        }

        _logger.LogTrace("Hashing password");

        var hashedPassword = _hasher.HashPassword(null!, password);

        _logger.LogTrace("Password hashed successfully");

        return hashedPassword;
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Password verification attempted with null or empty password");
            return false;
        }

        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            _logger.LogWarning("Password verification attempted with null or empty hashed password");
            return false;
        }

        _logger.LogTrace("Verifying password");

        var result = _hasher.VerifyHashedPassword(null!, hashedPassword, password);

        var isValid = result == PasswordVerificationResult.Success ||
                      result == PasswordVerificationResult.SuccessRehashNeeded;

        _logger.LogTrace("Password verification result: {Result}", result);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            _logger.LogInformation("Password verified but rehash is recommended");
        }

        return isValid;
    }

    /// <inheritdoc />
    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            _logger.LogWarning("Rehash check attempted with null or empty hashed password");
            return false;
        }

        try
        {
            // Verify with a dummy password to check if rehash is needed
            var result = _hasher.VerifyHashedPassword(null!, hashedPassword, "dummy");

            var needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;

            if (needsRehash)
            {
                _logger.LogDebug("Password hash needs rehashing");
            }

            return needsRehash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if password needs rehash");
            return false;
        }
    }
}
