using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
[Trait("Feature", "Identity")]
public sealed class UserRoleTests
{
    /// <summary>
    /// Confirms that the entity captures the required key relationships.
    /// </summary>
    [Fact]
    public void UserRole_WhenConstructed_SetsKeyProperties()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var role = new RoleBuilder().Build();

        // Act
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role,
            AssignedAtUtc = DateTime.UtcNow,
            AssignedBy = "Seeder"
        };

        // Assert
        userRole.UserId.Should().Be(user.Id);
        userRole.RoleId.Should().Be(role.Id);
        userRole.User.Should().Be(user);
        userRole.Role.Should().Be(role);
        userRole.AssignedBy.Should().Be("Seeder");
    }

    /// <summary>
    /// Validates that the association can be updated with a new timestamp.
    /// </summary>
    [Fact]
    public void UserRole_WhenAssignmentChanges_UpdatesTimestamp()
    {
        // Arrange
        var userRole = new UserRole
        {
            AssignedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var newTimestamp = DateTime.UtcNow;

        // Act
        userRole.AssignedAtUtc = newTimestamp;

        // Assert
        userRole.AssignedAtUtc.Should().Be(newTimestamp);
    }
}
