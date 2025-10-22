using System.Text.RegularExpressions;
using Archu.Application.Abstractions.Authentication;
using Archu.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Implementation of password validation service.
/// Validates passwords against configurable policy rules.
/// </summary>
public sealed class PasswordValidator : IPasswordValidator
{
    private readonly PasswordPolicyOptions _policy;
    private readonly ILogger<PasswordValidator> _logger;

    // Common weak passwords (top 100 most common passwords)
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "123456789", "12345678", "12345", "1234567", "password1",
        "123123", "1234567890", "000000", "abc123", "qwerty", "iloveyou", "welcome",
        "monkey", "dragon", "master", "sunshine", "princess", "login", "admin", "letmein",
        "solo", "passw0rd", "starwars", "hello", "freedom", "whatever", "trustno1",
        "qwertyuiop", "1q2w3e4r", "password123", "admin123", "welcome123", "test123",
        "root", "toor", "pass", "guest", "user", "demo", "temporary"
    };

    public PasswordValidator(
        IOptions<PasswordPolicyOptions> policyOptions,
        ILogger<PasswordValidator> logger)
    {
        _policy = policyOptions.Value;
        _logger = logger;

        // Validate policy on startup
        _policy.Validate();
    }

    /// <inheritdoc />
    public PasswordValidationResult ValidatePassword(string password, string? username = null, string? email = null)
    {
        if (string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Password validation attempted with null or empty password");
            return PasswordValidationResult.Failure("Password is required");
        }

        var errors = new List<string>();

        // 1. Check length requirements
        if (password.Length < _policy.MinimumLength)
        {
            errors.Add($"Password must be at least {_policy.MinimumLength} characters long");
        }

        if (password.Length > _policy.MaximumLength)
        {
            errors.Add($"Password cannot exceed {_policy.MaximumLength} characters");
        }

        // 2. Check character type requirements
        if (_policy.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter (A-Z)");
        }

        if (_policy.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter (a-z)");
        }

        if (_policy.RequireDigit && !Regex.IsMatch(password, @"\d"))
        {
            errors.Add("Password must contain at least one digit (0-9)");
        }

        if (_policy.RequireSpecialCharacter)
        {
            var specialCharsPattern = $"[{Regex.Escape(_policy.SpecialCharacters)}]";
            if (!Regex.IsMatch(password, specialCharsPattern))
            {
                errors.Add($"Password must contain at least one special character ({_policy.SpecialCharacters})");
            }
        }

        // 3. Check unique characters requirement
        if (_policy.MinimumUniqueCharacters > 0)
        {
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars < _policy.MinimumUniqueCharacters)
            {
                errors.Add($"Password must contain at least {_policy.MinimumUniqueCharacters} unique characters");
            }
        }

        // 4. Check against common passwords
        if (_policy.PreventCommonPasswords && IsCommonPassword(password))
        {
            errors.Add("Password is too common. Please choose a more secure password");
        }

        // 5. Check if password contains username or email
        if (_policy.PreventUserInfo)
        {
            if (!string.IsNullOrWhiteSpace(username) && 
                password.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Password cannot contain your username");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailLocalPart = email.Split('@')[0];
                if (password.Contains(emailLocalPart, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Password cannot contain your email address");
                }
            }
        }

        // If there are validation errors, return failure
        if (errors.Count > 0)
        {
            _logger.LogWarning("Password validation failed: {Errors}", string.Join(", ", errors));
            return PasswordValidationResult.Failure(errors);
        }

        // Calculate password strength
        var (strengthScore, strengthLevel) = CalculatePasswordStrength(password);

        _logger.LogInformation(
            "Password validated successfully. Strength: {StrengthLevel} ({StrengthScore}/100)",
            strengthLevel,
            strengthScore);

        return PasswordValidationResult.Success(strengthScore, strengthLevel);
    }

    /// <inheritdoc />
    public string GetPasswordRequirements()
    {
        return _policy.GetRequirementsDescription();
    }

    /// <summary>
    /// Checks if a password is in the common passwords list.
    /// </summary>
    private static bool IsCommonPassword(string password)
    {
        return CommonPasswords.Contains(password);
    }

    /// <summary>
    /// Calculates password strength score (0-100) and strength level.
    /// </summary>
    private (int score, PasswordStrengthLevel level) CalculatePasswordStrength(string password)
    {
        var score = 0;

        // Length contribution (max 30 points)
        if (password.Length >= 8) score += 10;
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;

        // Character variety contribution (max 40 points)
        if (Regex.IsMatch(password, @"[a-z]")) score += 10; // Lowercase
        if (Regex.IsMatch(password, @"[A-Z]")) score += 10; // Uppercase
        if (Regex.IsMatch(password, @"\d")) score += 10;     // Digits
        if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score += 10; // Special chars

        // Complexity contribution (max 30 points)
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars >= 6) score += 10;
        if (uniqueChars >= 10) score += 10;
        if (uniqueChars >= 15) score += 10;

        // Determine strength level
        var level = score switch
        {
            < 30 => PasswordStrengthLevel.VeryWeak,
            < 50 => PasswordStrengthLevel.Weak,
            < 70 => PasswordStrengthLevel.Fair,
            < 90 => PasswordStrengthLevel.Strong,
            _ => PasswordStrengthLevel.VeryStrong
        };

        return (score, level);
    }
}
