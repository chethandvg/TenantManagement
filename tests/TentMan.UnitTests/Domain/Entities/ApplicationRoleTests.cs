using TentMan.Domain.Entities.Identity;
using TentMan.UnitTests.TestHelpers.Fixtures;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace TentMan.UnitTests.Domain.Entities;

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
    [Theory, AutoMoqData]
    public void ApplicationRole_WhenConfigured_PersistsPropertyValues(Guid id, string name, string description, IFixture fixture)
    {
        // Arrange
        var normalizedName = name?.ToUpperInvariant() ?? string.Empty;
        var role = fixture.Build<ApplicationRole>()
            .With(r => r.Id, id)
            .With(r => r.Name, name ?? string.Empty)
            .With(r => r.NormalizedName, normalizedName)
            .With(r => r.Description, description)
            .Create();

        // Act & Assert
        role.Id.Should().Be(id);
        role.Name.Should().Be(name ?? string.Empty);
        role.NormalizedName.Should().Be(normalizedName);
        role.Description.Should().Be(description);
    }

    /// <summary>
    /// Ensures the navigation collection supports adding user role relationships.
    /// </summary>
    [Theory, AutoMoqData]
    public void UserRoles_WhenAddingRole_AssignsUser(IFixture fixture, DateTime assignedAtUtc, string assignedBy)
    {
        // Arrange
        var role = fixture.Create<ApplicationRole>();
        var user = fixture.Create<ApplicationUser>();
        var userRole = fixture.Build<UserRole>()
            .With(ur => ur.RoleId, role.Id)
            .With(ur => ur.UserId, user.Id)
            .With(ur => ur.Role, role)
            .With(ur => ur.User, user)
            .With(ur => ur.AssignedAtUtc, assignedAtUtc)
            .With(ur => ur.AssignedBy, assignedBy)
            .Create();

        // Act
        role.UserRoles.Add(userRole);

        // Assert
        role.UserRoles.Should().Contain(userRole);
    }
}
