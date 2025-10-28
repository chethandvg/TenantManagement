using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
[Trait("Feature", "Identity")]
public sealed class PasswordResetTokenTests
{
    /// <summary>
    /// Ensures tokens remain valid when unused, not revoked, and not expired.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsValid_WhenTokenActive_ReturnsTrue(IFixture fixture, DateTime currentTimeUtc)
    {
        // Arrange
        var normalizedTime = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        var token = fixture.Build<PasswordResetToken>()
            .With(t => t.ExpiresAtUtc, normalizedTime.AddMinutes(15))
            .With(t => t.IsUsed, false)
            .With(t => t.IsRevoked, false)
            .With(t => t.IsDeleted, false)
            .Create();

        // Act
        var result = token.IsValid(normalizedTime);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Ensures validation fails once the token is marked as used.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsValid_WhenTokenUsed_ReturnsFalse(IFixture fixture, DateTime currentTimeUtc)
    {
        // Arrange
        var normalizedTime = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        var usedAtUtc = normalizedTime.AddMinutes(-1);
        var token = fixture.Build<PasswordResetToken>()
            .With(t => t.ExpiresAtUtc, normalizedTime.AddMinutes(15))
            .With(t => t.IsUsed, true)
            .With(t => t.UsedAtUtc, usedAtUtc)
            .With(t => t.IsRevoked, false)
            .With(t => t.IsDeleted, false)
            .Create();

        // Act
        var result = token.IsValid(normalizedTime);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures validation fails once the token is revoked.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsValid_WhenTokenRevoked_ReturnsFalse(IFixture fixture, DateTime currentTimeUtc)
    {
        // Arrange
        var normalizedTime = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        var token = fixture.Build<PasswordResetToken>()
            .With(t => t.ExpiresAtUtc, normalizedTime.AddMinutes(15))
            .With(t => t.IsUsed, false)
            .With(t => t.IsRevoked, true)
            .With(t => t.IsDeleted, false)
            .Create();

        // Act
        var result = token.IsValid(normalizedTime);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures validation fails when the token has expired.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsValid_WhenTokenExpired_ReturnsFalse(IFixture fixture, DateTime currentTimeUtc)
    {
        // Arrange
        var normalizedTime = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        var token = fixture.Build<PasswordResetToken>()
            .With(t => t.ExpiresAtUtc, normalizedTime.AddMinutes(-5))
            .With(t => t.IsUsed, false)
            .With(t => t.IsRevoked, false)
            .With(t => t.IsDeleted, false)
            .Create();

        // Act
        var result = token.IsValid(normalizedTime);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Ensures validation fails when the token has been soft deleted.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsValid_WhenTokenDeleted_ReturnsFalse(IFixture fixture, DateTime currentTimeUtc, string deletedBy)
    {
        // Arrange
        var normalizedTime = DateTime.SpecifyKind(currentTimeUtc, DateTimeKind.Utc);
        var deletedAtUtc = normalizedTime.AddMinutes(-2);
        var token = fixture.Build<PasswordResetToken>()
            .With(t => t.ExpiresAtUtc, normalizedTime.AddMinutes(5))
            .With(t => t.IsUsed, false)
            .With(t => t.IsRevoked, false)
            .With(t => t.IsDeleted, true)
            .With(t => t.DeletedAtUtc, deletedAtUtc)
            .With(t => t.DeletedBy, deletedBy)
            .Create();

        // Act
        var result = token.IsValid(normalizedTime);

        // Assert
        result.Should().BeFalse();
    }
}
