using Archu.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Common.Behaviors;

[Trait("Category", "Unit")]
[Trait("Feature", "Behaviors")]
public class PerformanceBehaviorTests
{
    /// <summary>
    /// Ensures long-running requests trigger a warning log entry and that the delegate result is awaited.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogWarning_WhenRequestIsSlow()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<TestRequest, string>>>();
        var behavior = new PerformanceBehavior<TestRequest, string>(loggerMock.Object);

        var completionFlag = false;
        var request = new TestRequest();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var response = await behavior.Handle(
            request,
            async ct =>
            {
                ct.Should().Be(cancellationToken);
                await Task.Delay(TimeSpan.FromMilliseconds(550), ct);
                completionFlag = true;
                return "handled";
            },
            cancellationToken);

        // Assert
        response.Should().Be("handled");
        completionFlag.Should().BeTrue();

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Long Running Request")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures short-running requests do not trigger warning logs while still awaiting the delegate's execution.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldNotLogWarning_WhenRequestIsFast()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<TestRequest, string>>>();
        var behavior = new PerformanceBehavior<TestRequest, string>(loggerMock.Object);

        var completionFlag = false;
        var request = new TestRequest();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var response = await behavior.Handle(
            request,
            async ct =>
            {
                ct.Should().Be(cancellationToken);
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                completionFlag = true;
                return "handled";
            },
            cancellationToken);

        // Assert
        response.Should().Be("handled");
        completionFlag.Should().BeTrue();

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures that cancellation tokens propagate through awaited operations executed within the delegate.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<TestRequest, string>>>();
        var behavior = new PerformanceBehavior<TestRequest, string>(loggerMock.Object);

        var request = new TestRequest();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var cancellationToken = cancellationTokenSource.Token;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => behavior.Handle(
                request,
                async ct =>
                {
                    ct.Should().Be(cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    return "handled";
                },
                cancellationToken));

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    private sealed record TestRequest : IRequest<string>;
}
