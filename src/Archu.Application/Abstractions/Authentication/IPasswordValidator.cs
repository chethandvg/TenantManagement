namespace Archu.Application.Abstractions.Authentication;

/// <summary>
/// Service for validating passwords against defined policy rules.
/// </summary>
public interface IPasswordValidator
{
    /// <summary>
    /// Validates a password against the configured password policy.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="username">Optional username to check against (prevent password containing username).</param>
    /// <param name="email">Optional email to check against (prevent password containing email).</param>
    /// <returns>Validation result with success status and any error messages.</returns>
    PasswordValidationResult ValidatePassword(string password, string? username = null, string? email = null);

    /// <summary>
    /// Gets the password policy requirements description.
    /// </summary>
    string GetPasswordRequirements();
}
