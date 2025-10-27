using Archu.Domain.Entities.Identity;

namespace Archu.UnitTests.TestHelpers.Builders;

/// <summary>
/// Builder for creating <see cref="ApplicationRole"/> instances with descriptive defaults for tests.
/// </summary>
public sealed class RoleBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "User";
    private string _normalizedName = "USER";
    private string? _description = "Standard application role";
    private bool _isDeleted;
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private string? _createdBy = "System";
    private DateTime? _modifiedAtUtc;
    private string? _modifiedBy;
    private DateTime? _deletedAtUtc;
    private string? _deletedBy;
    private byte[] _rowVersion = Array.Empty<byte>();

    /// <summary>
    /// Configures the identifier used when building the role.
    /// </summary>
    /// <param name="id">The role identifier.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Configures the display name for the role.
    /// </summary>
    /// <param name="name">The display name value.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder WithName(string name)
    {
        _name = name;
        _normalizedName = name.ToUpperInvariant();
        return this;
    }

    /// <summary>
    /// Configures the optional description for the role.
    /// </summary>
    /// <param name="description">The description text.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Marks the role as deleted and records audit metadata.
    /// </summary>
    /// <param name="deletedAtUtc">The deletion timestamp.</param>
    /// <param name="deletedBy">The user who performed the deletion.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder AsDeleted(DateTime? deletedAtUtc = null, string? deletedBy = null)
    {
        _isDeleted = true;
        _deletedAtUtc = deletedAtUtc ?? DateTime.UtcNow;
        _deletedBy = deletedBy ?? "System";
        return this;
    }

    /// <summary>
    /// Configures the creation metadata for the role.
    /// </summary>
    /// <param name="createdAtUtc">Creation timestamp.</param>
    /// <param name="createdBy">The author identifier.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder WithCreatedInfo(DateTime createdAtUtc, string? createdBy)
    {
        _createdAtUtc = createdAtUtc;
        _createdBy = createdBy;
        return this;
    }

    /// <summary>
    /// Configures the modification metadata for the role.
    /// </summary>
    /// <param name="modifiedAtUtc">Modification timestamp.</param>
    /// <param name="modifiedBy">The modifier identifier.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder WithModifiedInfo(DateTime? modifiedAtUtc, string? modifiedBy)
    {
        _modifiedAtUtc = modifiedAtUtc;
        _modifiedBy = modifiedBy;
        return this;
    }

    /// <summary>
    /// Configures the row version for concurrency checks.
    /// </summary>
    /// <param name="rowVersion">The row version payload.</param>
    /// <returns>The updated builder instance.</returns>
    public RoleBuilder WithRowVersion(byte[] rowVersion)
    {
        _rowVersion = rowVersion;
        return this;
    }

    /// <summary>
    /// Builds the configured <see cref="ApplicationRole"/> instance.
    /// </summary>
    /// <returns>The constructed role entity.</returns>
    public ApplicationRole Build()
    {
        return new ApplicationRole
        {
            Id = _id,
            Name = _name,
            NormalizedName = _normalizedName,
            Description = _description,
            IsDeleted = _isDeleted,
            CreatedAtUtc = _createdAtUtc,
            CreatedBy = _createdBy,
            ModifiedAtUtc = _modifiedAtUtc,
            ModifiedBy = _modifiedBy,
            DeletedAtUtc = _deletedAtUtc,
            DeletedBy = _deletedBy,
            RowVersion = _rowVersion
        };
    }

    /// <summary>
    /// Creates a sequence of roles with generated names for collection-based tests.
    /// </summary>
    /// <param name="count">The number of roles to create.</param>
    /// <returns>A list of configured roles.</returns>
    public static List<ApplicationRole> CreateMany(int count = 3)
    {
        var roles = new List<ApplicationRole>();
        for (var i = 0; i < count; i++)
        {
            roles.Add(new RoleBuilder()
                .WithName($"Role{i + 1}")
                .Build());
        }

        return roles;
    }
}
