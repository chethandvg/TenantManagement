using Archu.Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Common.Behaviors;

[Trait("Category", "Unit")]
[Trait("Feature", "Behaviors")]
public class ValidationBehaviorTests
{
    /// <summary>
    /// Ensures successful validation invokes the next delegate and returns its response while forwarding the cancellation token to validators.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldInvokeNext_WhenValidationSucceeds()
    {
        // Arrange
        var request = new TestRequest("value");
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var validatorMock = new Mock<IValidator<TestRequest>>();
        var validationResult = new ValidationResult();

        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Returns<ValidationContext<TestRequest>, CancellationToken>((_, ct) =>
            {
                ct.Should().Be(cancellationToken);
                return Task.FromResult(validationResult);
            });

        var loggerMock = new Mock<ILogger<ValidationBehavior<TestRequest, string>>>();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validatorMock.Object }, loggerMock.Object);

        var nextInvoked = false;

        // Act
        var response = await behavior.Handle(
            request,
            ct =>
            {
                ct.Should().Be(cancellationToken);
                nextInvoked = true;
                return Task.FromResult("handled");
            },
            cancellationToken);

        // Assert
        response.Should().Be("handled");
        nextInvoked.Should().BeTrue();

        validatorMock.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken), Times.Once);
        loggerMock.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Ensures failed validation throws a ValidationException, logs the failure, and prevents the next delegate from executing.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        // Arrange
        var request = new TestRequest("invalid");
        var cancellationToken = CancellationToken.None;

        var validatorMock = new Mock<IValidator<TestRequest>>();
        var failures = new[] { new ValidationFailure("Property", "Error message") };

        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult(failures));

        var loggerMock = new Mock<ILogger<ValidationBehavior<TestRequest, string>>>();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validatorMock.Object }, loggerMock.Object);

        var nextInvoked = false;

        // Act
        var act = async () => await behavior.Handle(
            request,
            ct =>
            {
                nextInvoked = true;
                return Task.FromResult("handled");
            },
            cancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(act);
        exception.Errors.Should().Contain(f => f.PropertyName == "Property" && f.ErrorMessage == "Error message");
        nextInvoked.Should().BeFalse();

        validatorMock.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken), Times.Once);

        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures that cancellations propagate through validator execution and prevent the next delegate from running.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var request = new TestRequest("value");
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var cancellationToken = cancellationTokenSource.Token;

        var validatorMock = new Mock<IValidator<TestRequest>>();

        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .Returns<ValidationContext<TestRequest>, CancellationToken>(async (_, ct) =>
            {
                ct.Should().Be(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new ValidationResult();
            });

        var loggerMock = new Mock<ILogger<ValidationBehavior<TestRequest, string>>>();
        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validatorMock.Object }, loggerMock.Object);

        var nextInvoked = false;

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => behavior.Handle(
                request,
                ct =>
                {
                    ct.Should().Be(cancellationToken);
                    nextInvoked = true;
                    return Task.FromResult("handled");
                },
                cancellationToken));

        nextInvoked.Should().BeFalse();
        loggerMock.VerifyNoOtherCalls();
    }

    private sealed record TestRequest(string Value) : IRequest<string>;
}
