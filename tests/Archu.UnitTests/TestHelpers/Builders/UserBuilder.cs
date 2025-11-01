using Archu.Domain.Entities.Identity;

namespace Archu.UnitTests.TestHelpers.Builders;

/// <summary>
/// Builder pattern for creating ApplicationUser test data with sensible defaults.
/// Provides a fluent API for constructing ApplicationUser entities in tests.
/// </summary>
/// <example>
/// Usage:
/// <code>
/// // Simple user with defaults
/// var user = new UserBuilder().Build();
/// 
/// // Custom user
/// var admin = new UserBuilder()
///     .WithUserName("admin")
///     .WithEmail("admin@test.com")
///     .AsEmailConfirmed()
///     .Build();
/// 
/// // Multiple users
/// var users = UserBuilder.CreateMany(5);
/// </code>
/// </example>
public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _userName = "testuser";
    private string _email = "test@example.com";
    private string _normalizedEmail = "TEST@EXAMPLE.COM";
    private string _passwordHash = "hashedpassword123";
    private string _securityStamp = Guid.NewGuid().ToString();
    private byte[] _rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
    private bool _emailConfirmed = false;
    private bool _isDeleted = false;
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private string? _createdBy = "System";
    private DateTime? _modifiedAtUtc = null;
    private string? _modifiedBy = null;
    private DateTime? _deletedAtUtc = null;
    private string? _deletedBy = null;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        _normalizedEmail = email.ToUpperInvariant();
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithSecurityStamp(string securityStamp)
    {
        _securityStamp = securityStamp;
        return this;
    }

    public UserBuilder WithRowVersion(byte[] rowVersion)
    {
        _rowVersion = rowVersion;
        return this;
    }

    public UserBuilder AsEmailConfirmed()
    {
        _emailConfirmed = true;
        return this;
    }

    public UserBuilder AsDeleted(DateTime? deletedAtUtc = null, string? deletedBy = null)
    {
        _isDeleted = true;
        _deletedAtUtc = deletedAtUtc ?? DateTime.UtcNow;
        _deletedBy = deletedBy ?? "System";
        return this;
    }

    public UserBuilder WithCreatedInfo(DateTime createdAtUtc, string? createdBy)
    {
        _createdAtUtc = createdAtUtc;
        _createdBy = createdBy;
        return this;
    }

    public UserBuilder WithModifiedInfo(DateTime modifiedAtUtc, string? modifiedBy)
    {
        _modifiedAtUtc = modifiedAtUtc;
        _modifiedBy = modifiedBy;
        return this;
    }

    /// <summary>
    /// Builds an ApplicationUser entity with the configured values.
    /// </summary>
    public ApplicationUser Build()
    {
        return new ApplicationUser
        {
            Id = _id,
            UserName = _userName,
            Email = _email,
            NormalizedEmail = _normalizedEmail,
            PasswordHash = _passwordHash,
            SecurityStamp = _securityStamp,
            RowVersion = _rowVersion,
            EmailConfirmed = _emailConfirmed,
            IsDeleted = _isDeleted,
            CreatedAtUtc = _createdAtUtc,
            CreatedBy = _createdBy,
            ModifiedAtUtc = _modifiedAtUtc,
            ModifiedBy = _modifiedBy,
            DeletedAtUtc = _deletedAtUtc,
            DeletedBy = _deletedBy
        };
    }

    /// <summary>
    /// Creates a list of users with incremental usernames and emails.
    /// </summary>
    /// <param name="count">Number of users to create.</param>
    /// <returns>List of users with unique IDs and incremental values.</returns>
    public static List<ApplicationUser> CreateMany(int count = 3)
    {
        var users = new List<ApplicationUser>();
        for (int i = 0; i < count; i++)
        {
            users.Add(new UserBuilder()
                .WithUserName($"user{i + 1}")
                .WithEmail($"user{i + 1}@example.com")
                .Build());
        }
        return users;
    }

    /// <summary>
    /// Creates a list of email-confirmed users.
    /// </summary>
    /// <param name="count">Number of users to create.</param>
    /// <returns>List of email-confirmed users.</returns>
    public static List<ApplicationUser> CreateManyConfirmed(int count = 3)
    {
        var users = new List<ApplicationUser>();
        for (int i = 0; i < count; i++)
        {
            users.Add(new UserBuilder()
                .WithUserName($"user{i + 1}")
                .WithEmail($"user{i + 1}@example.com")
                .AsEmailConfirmed()
                .Build());
        }
        return users;
    }
}
