using Archu.Domain.Abstractions;
using Archu.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Common;

[Trait("Category", "Unit")]
[Trait("Feature", "DomainCommon")]
public sealed class BaseEntityTests
{
    /// <summary>
    /// Confirms that identifiers and concurrency tokens are initialized with safe defaults.
    /// </summary>
    [Fact]
    public void BaseEntity_WhenConstructed_InitializesDefaults()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBeEmpty();
        entity.RowVersion.Should().BeEmpty();
        entity.IsDeleted.Should().BeFalse();
        entity.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that soft delete metadata is captured when the entity is marked deleted.
    /// </summary>
    [Fact]
    public void BaseEntity_WhenSoftDeleted_TracksDeletionMetadata()
    {
        // Arrange
        var entity = new TestEntity();
        var deletedAt = DateTime.UtcNow;
        const string deletedBy = "UnitTest";

        // Act
        entity.IsDeleted = true;
        entity.DeletedAtUtc = deletedAt;
        entity.DeletedBy = deletedBy;

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAtUtc.Should().Be(deletedAt);
        entity.DeletedBy.Should().Be(deletedBy);
    }

    /// <summary>
    /// Verifies that audit metadata can be updated.
    /// </summary>
    [Fact]
    public void BaseEntity_WhenAudited_TracksModificationMetadata()
    {
        // Arrange
        var entity = new TestEntity();
        var createdAt = DateTime.UtcNow.AddHours(-1);
        const string createdBy = "Seeder";
        var modifiedAt = DateTime.UtcNow;
        const string modifiedBy = "Updater";

        // Act
        entity.CreatedAtUtc = createdAt;
        entity.CreatedBy = createdBy;
        entity.ModifiedAtUtc = modifiedAt;
        entity.ModifiedBy = modifiedBy;

        // Assert
        entity.CreatedAtUtc.Should().Be(createdAt);
        entity.CreatedBy.Should().Be(createdBy);
        entity.ModifiedAtUtc.Should().Be(modifiedAt);
        entity.ModifiedBy.Should().Be(modifiedBy);
    }

    /// <summary>
    /// Ensures the base entity implements the expected domain interfaces.
    /// </summary>
    [Fact]
    public void BaseEntity_ImplementsAuditableAndSoftDeleteInterfaces()
    {
        // Arrange
        var entity = new TestEntity();

        // Act & Assert
        entity.Should().BeAssignableTo<IAuditable>();
        entity.Should().BeAssignableTo<ISoftDeletable>();
    }

    private sealed class TestEntity : BaseEntity
    {
    }
}
