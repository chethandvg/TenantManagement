using Archu.Application.Common;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Application.Common;

[Trait("Category", "Unit")]
[Trait("Feature", "ResultPattern")]
public sealed class ResultTests
{
    /// <summary>
    /// Verifies that successful generic results expose the expected value and flags.
    /// </summary>
    [Fact]
    public void ResultOfT_Success_SetsSuccessProperties()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = Result<int>.Success(expectedValue);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(expectedValue);
        result.Error.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    /// <summary>
    /// Verifies that failed generic results expose the expected error information.
    /// </summary>
    [Fact]
    public void ResultOfT_Failure_SetsErrorInformation()
    {
        // Arrange
        const string error = "Something went wrong";

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().Be(error);
        result.Errors.Should().BeNull();
    }

    /// <summary>
    /// Verifies that failed generic results can surface multiple errors.
    /// </summary>
    [Fact]
    public void ResultOfT_FailureWithErrors_SetsErrorCollection()
    {
        // Arrange
        var errors = new[] { "Error1", "Error2" };

        // Act
        var result = Result<int>.Failure("Composite error", errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Composite error");
        result.Errors.Should().Contain(errors);
    }

    /// <summary>
    /// Verifies that successful non-generic results expose the expected flags.
    /// </summary>
    [Fact]
    public void Result_Success_SetsSuccessProperties()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    /// <summary>
    /// Verifies that failed non-generic results expose the expected error message.
    /// </summary>
    [Fact]
    public void Result_Failure_SetsErrorInformation()
    {
        // Arrange
        const string error = "Operation failed";

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Errors.Should().BeNull();
    }

    /// <summary>
    /// Verifies that failed non-generic results can surface multiple errors.
    /// </summary>
    [Fact]
    public void Result_FailureWithErrors_SetsErrorCollection()
    {
        // Arrange
        var errors = new[] { "ErrorA", "ErrorB" };

        // Act
        var result = Result.Failure("Composite failure", errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Composite failure");
        result.Errors.Should().Contain(errors);
    }
}
