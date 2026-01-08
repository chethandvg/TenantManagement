using TentMan.Domain.Entities.Identity;
using TentMan.UnitTests.TestHelpers.Fixtures;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace TentMan.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
[Trait("Feature", "Identity")]
public sealed class ApplicationUserTests
{
    /// <summary>
    /// Verifies that the default constructor populates required collections and identifiers.
    /// </summary>
    [Fact]
    public void ApplicationUser_WhenCreated_InitializesDefaults()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.Id.Should().NotBeEmpty();
        user.UserRoles.Should().NotBeNull();
        user.RowVersion.Should().BeEmpty();
        user.IsDeleted.Should().BeFalse();
    }

    /// <summary>
    /// Ensures the lockout helper returns false when lockout is disabled.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsLockedOut_WhenLockoutDisabled_ReturnsFalse(IFixture fixture)
    {
        // Arrange
        var user = fixture.Build<ApplicationUser>()
            .With(u => u.LockoutEnabled, false)
            .With(u => u.LockoutEnd, DateTime.UtcNow.AddMinutes(5))
            .Create();

        // Act
        var result = user.IsLockedOut;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures the lockout helper returns false when the lockout period has expired.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsLockedOut_WhenLockoutExpired_ReturnsFalse(IFixture fixture)
    {
        // Arrange
        var user = fixture.Build<ApplicationUser>()
            .With(u => u.LockoutEnabled, true)
            .With(u => u.LockoutEnd, DateTime.UtcNow.AddMinutes(-1))
            .Create();

        // Act
        var result = user.IsLockedOut;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures the lockout helper returns true while the user is actively locked out.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsLockedOut_WhenLockoutActive_ReturnsTrue(IFixture fixture)
    {
        // Arrange
        var user = fixture.Build<ApplicationUser>()
            .With(u => u.LockoutEnabled, true)
            .With(u => u.LockoutEnd, DateTime.UtcNow.AddMinutes(10))
            .Create();

        // Act
        var result = user.IsLockedOut;

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Validates the overload accepting a custom timestamp for deterministic testing.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsLockedOutAt_WithCustomTimestamp_UsesProvidedTime(IFixture fixture)
    {
        // Arrange
        var evaluationTime = DateTime.UtcNow.AddMinutes(1);
        var user = fixture.Build<ApplicationUser>()
            .With(u => u.LockoutEnabled, true)
            .With(u => u.LockoutEnd, evaluationTime.AddMinutes(4))
            .Create();

        // Act
        var result = user.IsLockedOutAt(evaluationTime);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Confirms the navigation collection can accept user role assignments.
    /// </summary>
    [Theory, AutoMoqData]
    public void UserRoles_WhenAddingRole_AddsEntry(IFixture fixture, DateTime assignedAtUtc)
    {
        // Arrange
        var user = fixture.Create<ApplicationUser>();
        var role = fixture.Create<ApplicationRole>();

        var userRole = fixture.Build<UserRole>()
            .With(ur => ur.UserId, user.Id)
            .With(ur => ur.RoleId, role.Id)
            .With(ur => ur.User, user)
            .With(ur => ur.Role, role)
            .With(ur => ur.AssignedAtUtc, assignedAtUtc)
            .Create();

        // Act
        user.UserRoles.Add(userRole);

        // Assert
        user.UserRoles.Should().Contain(userRole);
    }
}
