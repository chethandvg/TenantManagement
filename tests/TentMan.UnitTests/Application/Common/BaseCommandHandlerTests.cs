using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using System.Collections.Generic;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Common;

/// <summary>
/// Unit tests for BaseCommandHandler to verify shared authentication and logging behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Common")]
public class BaseCommandHandlerTests
{
    [Fact]
    public void CreateHandler_WhenHandlerRequiresCurrentUserAndNonGenericLogger_ReturnsConfiguredHandler()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithAuthenticatedUser(expectedUserId);

        // Act
        var handler = fixture.CreateHandler();

        // Assert
        handler.TestGetCurrentUserId().Should().Be(expectedUserId);
    }

    #region GetCurrentUserId Tests

    [Fact]
    public void GetCurrentUserId_WhenUserIsAuthenticated_ReturnsUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithAuthenticatedUser(expectedUserId);

        var handler = fixture.CreateHandler();

        // Act
        var result = handler.TestGetCurrentUserId();

        // Assert
        result.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetCurrentUserId_WhenUserIsNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => handler.TestGetCurrentUserId());

        exception.Message.Should().Contain("User must be authenticated to this operation");
    }

    [Fact]
    public void GetCurrentUserId_WithOperationName_ThrowsUnauthorizedAccessExceptionWithOperationName()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => handler.TestGetCurrentUserIdWithOperation("delete products"));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Fact]
    public void GetCurrentUserId_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithInvalidUserIdFormat("not-a-valid-guid");

        var handler = fixture.CreateHandler();

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => handler.TestGetCurrentUserId());

        exception.Message.Should().Contain("User must be authenticated to this operation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-guid-format")]
    [InlineData("12345")]
    [InlineData("GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG")]
    public void GetCurrentUserId_WhenUserIdHasInvalidFormat_ThrowsUnauthorizedAccessException(string invalidUserId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = fixture.CreateHandler();

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(
            () => handler.TestGetCurrentUserId());

        exception.Message.Should().Contain("User must be authenticated to this operation");
    }

    [Fact]
    public void GetCurrentUserId_LogsErrorBeforeThrowing()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(
            () => handler.TestGetCurrentUserId());

        fixture.VerifyStructuredErrorLogged(new Dictionary<string, object?>
        {
            { "Operation", "this operation" },
            { "{OriginalFormat}", "Cannot perform {Operation}: User ID not found or invalid" }
        });
    }

    [Fact]
    public void GetCurrentUserId_LogsErrorWithOperationName()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(
            () => handler.TestGetCurrentUserIdWithOperation("create products"));

        fixture.VerifyStructuredErrorLogged(new Dictionary<string, object?>
        {
            { "Operation", "create products" },
            { "{OriginalFormat}", "Cannot perform {Operation}: User ID not found or invalid" }
        });
    }

    #endregion

    #region TryGetCurrentUserId Tests

    [Fact]
    public void TryGetCurrentUserId_WhenUserIsAuthenticated_ReturnsTrueAndUserId()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithAuthenticatedUser(expectedUserId);

        var handler = fixture.CreateHandler();

        // Act
        var result = handler.TestTryGetCurrentUserId(out var userId);

        // Assert
        result.Should().BeTrue();
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void TryGetCurrentUserId_WhenUserIsNotAuthenticated_ReturnsFalseAndEmptyGuid()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();

        // Act
        var result = handler.TestTryGetCurrentUserId(out var userId);

        // Assert
        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetCurrentUserId_WhenUserIdIsInvalidGuid_ReturnsFalseAndEmptyGuid()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithInvalidUserIdFormat("not-a-valid-guid");

        var handler = fixture.CreateHandler();

        // Act
        var result = handler.TestTryGetCurrentUserId(out var userId);

        // Assert
        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetCurrentUserId_WhenUserIdIsEmptyString_ReturnsFalseAndEmptyGuid()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithInvalidUserIdFormat(string.Empty);

        var handler = fixture.CreateHandler();

        // Act
        var result = handler.TestTryGetCurrentUserId(out var userId);

        // Assert
        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetCurrentUserId_DoesNotLogOrThrow()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<TestCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();

        // Act
        var result = handler.TestTryGetCurrentUserId(out var _);

        // Assert
        result.Should().BeFalse();
        
        // Verify no logs were written
        fixture.MockLogger.VerifyNoOtherCalls();
    }

    #endregion

    #region Test Helper Handler

    /// <summary>
    /// Concrete test handler to expose protected BaseCommandHandler methods for testing.
    /// </summary>
    public class TestCommandHandler : BaseCommandHandler
    {
        public TestCommandHandler(ICurrentUser currentUser, ILogger logger)
            : base(currentUser, logger)
        {
        }

        public Guid TestGetCurrentUserId() => GetCurrentUserId();

        public Guid TestGetCurrentUserIdWithOperation(string operation) => GetCurrentUserId(operation);

        public bool TestTryGetCurrentUserId(out Guid userId) => TryGetCurrentUserId(out userId);
    }

    #endregion
}
