using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
[Trait("Feature", "Identity")]
public sealed class ApplicationRoleTests
{
    /// <summary>
    /// Validates default values created by the constructor.
    /// </summary>
    [Fact]
    public void ApplicationRole_WhenCreated_InitializesDefaults()
    {
        // Arrange & Act
        var role = new ApplicationRole();

        // Assert
        role.Id.Should().NotBeEmpty();
        role.UserRoles.Should().NotBeNull();
        role.RowVersion.Should().BeEmpty();
        role.IsDeleted.Should().BeFalse();
    }

    /// <summary>
    /// Confirms that name and description properties can be customized.
    /// </summary>
    [Fact]
    public void ApplicationRole_WhenConfigured_PersistsPropertyValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var role = new RoleBuilder()
            .WithId(id)
            .WithName("Manager")
            .WithDescription("Can manage departmental data")
            .Build();

        // Act & Assert
        role.Id.Should().Be(id);
        role.Name.Should().Be("Manager");
        role.NormalizedName.Should().Be("MANAGER");
        role.Description.Should().Be("Can manage departmental data");
    }

    /// <summary>
    /// Ensures the navigation collection supports adding user role relationships.
    /// </summary>
    [Fact]
    public void UserRoles_WhenAddingRole_AssignsUser()
    {
        // Arrange
        var role = new RoleBuilder().Build();
        var user = new UserBuilder().Build();
        var userRole = new UserRole
        {
            RoleId = role.Id,
            UserId = user.Id,
            Role = role,
            User = user,
            AssignedAtUtc = DateTime.UtcNow
        };

        // Act
        role.UserRoles.Add(userRole);

        // Assert
        role.UserRoles.Should().Contain(userRole);
    }
}
