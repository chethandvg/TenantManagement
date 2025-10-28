using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using AutoFixture;
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
    [Theory, AutoMoqData]
    public void UserRole_WhenConstructed_SetsKeyProperties(IFixture fixture, DateTime assignedAtUtc, string assignedBy)
    {
        // Arrange
        var user = fixture.Create<ApplicationUser>();
        var role = fixture.Create<ApplicationRole>();

        // Act
        var userRole = fixture.Build<UserRole>()
            .With(ur => ur.UserId, user.Id)
            .With(ur => ur.RoleId, role.Id)
            .With(ur => ur.User, user)
            .With(ur => ur.Role, role)
            .With(ur => ur.AssignedAtUtc, assignedAtUtc)
            .With(ur => ur.AssignedBy, assignedBy)
            .Create();

        // Assert
        userRole.UserId.Should().Be(user.Id);
        userRole.RoleId.Should().Be(role.Id);
        userRole.User.Should().Be(user);
        userRole.Role.Should().Be(role);
        userRole.AssignedBy.Should().Be(assignedBy);
    }

    /// <summary>
    /// Validates that the association can be updated with a new timestamp.
    /// </summary>
    [Theory, AutoMoqData]
    public void UserRole_WhenAssignmentChanges_UpdatesTimestamp(DateTime newTimestamp)
    {
        // Arrange
        var userRole = new UserRole
        {
            AssignedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        userRole.AssignedAtUtc = newTimestamp;

        // Assert
        userRole.AssignedAtUtc.Should().Be(newTimestamp);
    }
}
