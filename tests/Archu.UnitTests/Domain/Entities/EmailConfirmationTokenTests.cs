using Archu.UnitTests.TestHelpers.Builders;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
[Trait("Feature", "Identity")]
public sealed class EmailConfirmationTokenTests
{
    /// <summary>
    /// Ensures tokens remain valid when unused, not revoked, and not expired.
    /// </summary>
    [Fact]
    public void IsValid_WhenTokenActive_ReturnsTrue()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var token = new UserTokenBuilder()
            .WithExpiration(currentTime.AddHours(2))
            .BuildEmailConfirmationToken();

        // Act
        var result = token.IsValid(currentTime);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Ensures validation fails once the token is used.
    /// </summary>
    [Fact]
    public void IsValid_WhenTokenUsed_ReturnsFalse()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var token = new UserTokenBuilder()
            .WithExpiration(currentTime.AddHours(2))
            .AsUsed(currentTime.AddMinutes(-5))
            .BuildEmailConfirmationToken();

        // Act
        var result = token.IsValid(currentTime);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures validation fails once the token is revoked.
    /// </summary>
    [Fact]
    public void IsValid_WhenTokenRevoked_ReturnsFalse()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var token = new UserTokenBuilder()
            .WithExpiration(currentTime.AddHours(2))
            .AsRevoked()
            .BuildEmailConfirmationToken();

        // Act
        var result = token.IsValid(currentTime);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures validation fails when the token expires.
    /// </summary>
    [Fact]
    public void IsValid_WhenTokenExpired_ReturnsFalse()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var token = new UserTokenBuilder()
            .WithExpiration(currentTime.AddMinutes(-30))
            .BuildEmailConfirmationToken();

        // Act
        var result = token.IsValid(currentTime);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures validation fails when the token has been soft deleted.
    /// </summary>
    [Fact]
    public void IsValid_WhenTokenDeleted_ReturnsFalse()
    {
        // Arrange
        var currentTime = DateTime.UtcNow;
        var token = new UserTokenBuilder()
            .WithExpiration(currentTime.AddHours(2))
            .AsDeleted(currentTime.AddMinutes(-1), "Admin")
            .BuildEmailConfirmationToken();

        // Act
        var result = token.IsValid(currentTime);

        // Assert
        result.Should().BeFalse();
    }
}
