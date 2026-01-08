using TentMan.Domain.Entities.Identity;

namespace TentMan.UnitTests.TestHelpers.Builders;

/// <summary>
/// Builder for creating password reset and email confirmation tokens for identity tests.
/// </summary>
public sealed class UserTokenBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private string _token = Guid.NewGuid().ToString("N");
    private DateTime _expiresAtUtc = DateTime.UtcNow.AddHours(1);
    private bool _isUsed;
    private bool _isRevoked;
    private bool _isDeleted;
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private string? _createdBy = "System";
    private DateTime? _modifiedAtUtc;
    private string? _modifiedBy;
    private DateTime? _deletedAtUtc;
    private string? _deletedBy;
    private DateTime? _usedAtUtc;
    private ApplicationUser _user = new UserBuilder().Build();
    private byte[] _rowVersion = Array.Empty<byte>();

    /// <summary>
    /// Configures the identifier applied to the next token build.
    /// </summary>
    /// <param name="id">The token identifier value.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Configures the owning user identifier for the token.
    /// </summary>
    /// <param name="userId">The owner identifier.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Configures the raw token value for verification tests.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithToken(string token)
    {
        _token = token;
        return this;
    }

    /// <summary>
    /// Configures the token expiration time.
    /// </summary>
    /// <param name="expiresAtUtc">The expiration timestamp.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithExpiration(DateTime expiresAtUtc)
    {
        _expiresAtUtc = expiresAtUtc;
        return this;
    }

    /// <summary>
    /// Marks the token as used and records when usage occurred.
    /// </summary>
    /// <param name="usedAtUtc">The timestamp when the token was used.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder AsUsed(DateTime? usedAtUtc = null)
    {
        _isUsed = true;
        _usedAtUtc = usedAtUtc ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the token as revoked.
    /// </summary>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder AsRevoked()
    {
        _isRevoked = true;
        return this;
    }

    /// <summary>
    /// Marks the token as deleted with audit metadata.
    /// </summary>
    /// <param name="deletedAtUtc">The deletion timestamp.</param>
    /// <param name="deletedBy">The user responsible for the deletion.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder AsDeleted(DateTime? deletedAtUtc = null, string? deletedBy = null)
    {
        _isDeleted = true;
        _deletedAtUtc = deletedAtUtc ?? DateTime.UtcNow;
        _deletedBy = deletedBy ?? "System";
        return this;
    }

    /// <summary>
    /// Provides the navigation user instance for the token.
    /// </summary>
    /// <param name="user">The associated <see cref="ApplicationUser"/>.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithUser(ApplicationUser user)
    {
        _user = user;
        return this;
    }

    /// <summary>
    /// Configures the creation metadata.
    /// </summary>
    /// <param name="createdAtUtc">The creation timestamp.</param>
    /// <param name="createdBy">The author identifier.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithCreatedInfo(DateTime createdAtUtc, string? createdBy)
    {
        _createdAtUtc = createdAtUtc;
        _createdBy = createdBy;
        return this;
    }

    /// <summary>
    /// Configures the modification metadata.
    /// </summary>
    /// <param name="modifiedAtUtc">The modification timestamp.</param>
    /// <param name="modifiedBy">The modifier identifier.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithModifiedInfo(DateTime? modifiedAtUtc, string? modifiedBy)
    {
        _modifiedAtUtc = modifiedAtUtc;
        _modifiedBy = modifiedBy;
        return this;
    }

    /// <summary>
    /// Configures the concurrency token used for optimistic locking tests.
    /// </summary>
    /// <param name="rowVersion">The concurrency token payload.</param>
    /// <returns>The updated builder instance.</returns>
    public UserTokenBuilder WithRowVersion(byte[] rowVersion)
    {
        _rowVersion = rowVersion;
        return this;
    }

    /// <summary>
    /// Builds a configured <see cref="PasswordResetToken"/> instance.
    /// </summary>
    /// <returns>The constructed password reset token.</returns>
    public PasswordResetToken BuildPasswordResetToken()
    {
        return new PasswordResetToken
        {
            Id = _id,
            UserId = _userId,
            Token = _token,
            ExpiresAtUtc = _expiresAtUtc,
            IsUsed = _isUsed,
            IsRevoked = _isRevoked,
            IsDeleted = _isDeleted,
            CreatedAtUtc = _createdAtUtc,
            CreatedBy = _createdBy,
            ModifiedAtUtc = _modifiedAtUtc,
            ModifiedBy = _modifiedBy,
            DeletedAtUtc = _deletedAtUtc,
            DeletedBy = _deletedBy,
            UsedAtUtc = _usedAtUtc,
            User = _user,
            RowVersion = _rowVersion
        };
    }

    /// <summary>
    /// Builds a configured <see cref="EmailConfirmationToken"/> instance.
    /// </summary>
    /// <returns>The constructed email confirmation token.</returns>
    public EmailConfirmationToken BuildEmailConfirmationToken()
    {
        return new EmailConfirmationToken
        {
            Id = _id,
            UserId = _userId,
            Token = _token,
            ExpiresAtUtc = _expiresAtUtc,
            IsUsed = _isUsed,
            IsRevoked = _isRevoked,
            IsDeleted = _isDeleted,
            CreatedAtUtc = _createdAtUtc,
            CreatedBy = _createdBy,
            ModifiedAtUtc = _modifiedAtUtc,
            ModifiedBy = _modifiedBy,
            DeletedAtUtc = _deletedAtUtc,
            DeletedBy = _deletedBy,
            UsedAtUtc = _usedAtUtc,
            User = _user,
            RowVersion = _rowVersion
        };
    }
}
