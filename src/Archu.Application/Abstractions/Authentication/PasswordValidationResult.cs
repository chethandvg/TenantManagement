namespace Archu.Application.Abstractions.Authentication;

/// <summary>
/// Result of password validation.
/// </summary>
public sealed class PasswordValidationResult
{
    /// <summary>
    /// Gets whether the password is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the password strength score (0-100).
    /// </summary>
    public int StrengthScore { get; init; }

    /// <summary>
    /// Gets the password strength level.
    /// </summary>
    public PasswordStrengthLevel StrengthLevel { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static PasswordValidationResult Success(int strengthScore, PasswordStrengthLevel strengthLevel)
    {
        return new PasswordValidationResult
        {
            IsValid = true,
            StrengthScore = strengthScore,
            StrengthLevel = strengthLevel
        };
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static PasswordValidationResult Failure(params string[] errors)
    {
        return new PasswordValidationResult
        {
            IsValid = false,
            Errors = errors,
            StrengthScore = 0,
            StrengthLevel = PasswordStrengthLevel.VeryWeak
        };
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static PasswordValidationResult Failure(IEnumerable<string> errors)
    {
        return new PasswordValidationResult
        {
            IsValid = false,
            Errors = errors.ToArray(),
            StrengthScore = 0,
            StrengthLevel = PasswordStrengthLevel.VeryWeak
        };
    }
}

/// <summary>
/// Password strength levels.
/// </summary>
public enum PasswordStrengthLevel
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Strong = 3,
    VeryStrong = 4
}
