namespace Archu.Domain.ValueObjects;

/// <summary>
/// Value object representing password policy requirements.
/// </summary>
public sealed class PasswordPolicyOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "PasswordPolicy";

    /// <summary>
    /// Minimum password length. Default: 8 characters.
    /// </summary>
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Maximum password length. Default: 128 characters.
    /// </summary>
    public int MaximumLength { get; set; } = 128;

    /// <summary>
    /// Require at least one uppercase letter (A-Z).
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Require at least one lowercase letter (a-z).
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Require at least one digit (0-9).
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Require at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?).
    /// </summary>
    public bool RequireSpecialCharacter { get; set; } = true;

    /// <summary>
    /// Minimum number of unique characters required.
    /// Helps prevent passwords like "aaaa1234!A".
    /// Default: 0 (disabled).
    /// </summary>
    public int MinimumUniqueCharacters { get; set; } = 0;

    /// <summary>
    /// Prevent common passwords (e.g., "password", "123456").
    /// Default: true.
    /// </summary>
    public bool PreventCommonPasswords { get; set; } = true;

    /// <summary>
    /// Prevent passwords that contain the username or email.
    /// Default: true.
    /// </summary>
    public bool PreventUserInfo { get; set; } = true;

    /// <summary>
    /// List of special characters considered valid.
    /// </summary>
    public string SpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;:,.<>?~`";

    /// <summary>
    /// Validates that the policy configuration is valid.
    /// </summary>
    public void Validate()
    {
        if (MinimumLength < 4)
            throw new InvalidOperationException("Minimum password length must be at least 4 characters.");

        if (MaximumLength < MinimumLength)
            throw new InvalidOperationException("Maximum password length must be greater than or equal to minimum length.");

        if (MaximumLength > 256)
            throw new InvalidOperationException("Maximum password length cannot exceed 256 characters.");

        if (MinimumUniqueCharacters < 0)
            throw new InvalidOperationException("Minimum unique characters cannot be negative.");

        if (MinimumUniqueCharacters > MinimumLength)
            throw new InvalidOperationException("Minimum unique characters cannot exceed minimum length.");
    }

    /// <summary>
    /// Gets a human-readable description of the password requirements.
    /// </summary>
    public string GetRequirementsDescription()
    {
        var requirements = new List<string>
        {
            $"Be between {MinimumLength} and {MaximumLength} characters long"
        };

        if (RequireUppercase)
            requirements.Add("Contain at least one uppercase letter (A-Z)");

        if (RequireLowercase)
            requirements.Add("Contain at least one lowercase letter (a-z)");

        if (RequireDigit)
            requirements.Add("Contain at least one digit (0-9)");

        if (RequireSpecialCharacter)
            requirements.Add($"Contain at least one special character ({SpecialCharacters})");

        if (MinimumUniqueCharacters > 0)
            requirements.Add($"Contain at least {MinimumUniqueCharacters} unique characters");

        if (PreventCommonPasswords)
            requirements.Add("Not be a commonly used password");

        if (PreventUserInfo)
            requirements.Add("Not contain your username or email");

        return string.Join("\n", requirements.Select((r, i) => $"{i + 1}. {r}"));
    }
}
