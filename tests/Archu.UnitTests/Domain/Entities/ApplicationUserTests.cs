using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Entities;

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
    [Fact]
    public void IsLockedOut_WhenLockoutDisabled_ReturnsFalse()
    {
        // Arrange
        var user = new UserBuilder()
            .Build();

        user.LockoutEnabled = false;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = user.IsLockedOut;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures the lockout helper returns false when the lockout period has expired.
    /// </summary>
    [Fact]
    public void IsLockedOut_WhenLockoutExpired_ReturnsFalse()
    {
        // Arrange
        var user = new UserBuilder()
            .Build();

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var result = user.IsLockedOut;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures the lockout helper returns true while the user is actively locked out.
    /// </summary>
    [Fact]
    public void IsLockedOut_WhenLockoutActive_ReturnsTrue()
    {
        // Arrange
        var user = new UserBuilder()
            .Build();

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);

        // Act
        var result = user.IsLockedOut;

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Validates the overload accepting a custom timestamp for deterministic testing.
    /// </summary>
    [Fact]
    public void IsLockedOutAt_WithCustomTimestamp_UsesProvidedTime()
    {
        // Arrange
        var user = new UserBuilder()
            .Build();

        var lockoutStarted = DateTime.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = lockoutStarted.AddMinutes(5);
        var evaluationTime = lockoutStarted.AddMinutes(1);

        // Act
        var result = user.IsLockedOutAt(evaluationTime);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Confirms the navigation collection can accept user role assignments.
    /// </summary>
    [Fact]
    public void UserRoles_WhenAddingRole_AddsEntry()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var role = new RoleBuilder().Build();

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role,
            AssignedAtUtc = DateTime.UtcNow
        };

        // Act
        user.UserRoles.Add(userRole);

        // Assert
        user.UserRoles.Should().Contain(userRole);
    }
}
