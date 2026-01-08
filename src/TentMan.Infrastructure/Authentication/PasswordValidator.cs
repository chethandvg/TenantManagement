using System.Text.RegularExpressions;
using TentMan.Application.Abstractions.Authentication;
using TentMan.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.Infrastructure.Authentication;

/// <summary>
/// Implementation of password validation service.
/// Validates passwords against configurable policy rules.
/// </summary>
public sealed partial class PasswordValidator : IPasswordValidator
{
    private readonly PasswordPolicyOptions _policy;
    private readonly ILogger<PasswordValidator> _logger;
    private readonly Regex? _specialCharsPattern;

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

    // Compiled regex patterns for performance
    [GeneratedRegex(@"[A-Z]", RegexOptions.Compiled)]
    private static partial Regex UppercasePattern();

    [GeneratedRegex(@"[a-z]", RegexOptions.Compiled)]
    private static partial Regex LowercasePattern();

    [GeneratedRegex(@"\d", RegexOptions.Compiled)]
    private static partial Regex DigitPattern();

    [GeneratedRegex(@"[^a-zA-Z0-9]", RegexOptions.Compiled)]
    private static partial Regex SpecialCharPatternGeneric();

    public PasswordValidator(
        IOptions<PasswordPolicyOptions> policyOptions,
        ILogger<PasswordValidator> logger)
    {
        _policy = policyOptions.Value;
        _logger = logger;

        // Validate policy on startup
        _policy.Validate();

        // Compile special chars pattern once if special characters are required
        if (_policy.RequireSpecialCharacter)
        {
            var specialCharsRegex = $"[{Regex.Escape(_policy.SpecialCharacters)}]";
            _specialCharsPattern = new Regex(specialCharsRegex, RegexOptions.Compiled);
        }
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

        // 2. Check character type requirements using compiled regex
        if (_policy.RequireUppercase && !UppercasePattern().IsMatch(password))
        {
            errors.Add("Password must contain at least one uppercase letter (A-Z)");
        }

        if (_policy.RequireLowercase && !LowercasePattern().IsMatch(password))
        {
            errors.Add("Password must contain at least one lowercase letter (a-z)");
        }

        if (_policy.RequireDigit && !DigitPattern().IsMatch(password))
        {
            errors.Add("Password must contain at least one digit (0-9)");
        }

        if (_policy.RequireSpecialCharacter && _specialCharsPattern != null)
        {
            if (!_specialCharsPattern.IsMatch(password))
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
    /// Calculates password strength score (0-100) and strength level using compiled regex.
    /// </summary>
    private (int score, PasswordStrengthLevel level) CalculatePasswordStrength(string password)
    {
        var score = 0;

        // Length contribution (max 30 points)
        if (password.Length >= 8) score += 10;
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;

        // Character variety contribution (max 40 points) using compiled regex
        if (LowercasePattern().IsMatch(password)) score += 10; // Lowercase
        if (UppercasePattern().IsMatch(password)) score += 10; // Uppercase
        if (DigitPattern().IsMatch(password)) score += 10;     // Digits
        if (SpecialCharPatternGeneric().IsMatch(password)) score += 10; // Special chars

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
