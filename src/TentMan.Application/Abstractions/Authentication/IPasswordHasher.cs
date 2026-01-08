namespace TentMan.Application.Abstractions.Authentication;

/// <summary>
/// Service for hashing and verifying passwords.
/// Abstracts the password hashing implementation to allow different algorithms.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using a secure one-way algorithm.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plain text password matches a hashed password.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the password matches; otherwise, false.</returns>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Checks if a hashed password needs to be rehashed (e.g., using outdated algorithm or parameters).
    /// </summary>
    /// <param name="hashedPassword">The hashed password to check.</param>
    /// <returns>True if the password should be rehashed; otherwise, false.</returns>
    bool NeedsRehash(string hashedPassword);
}
