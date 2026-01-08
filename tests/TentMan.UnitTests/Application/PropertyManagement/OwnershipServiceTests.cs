using TentMan.Application.PropertyManagement.Services;
using Xunit;

namespace TentMan.UnitTests.Application.PropertyManagement;

public class OwnershipServiceTests
{
    private readonly OwnershipService _service;

    public OwnershipServiceTests()
    {
        _service = new OwnershipService();
    }

    [Fact]
    public void ValidateOwnershipShares_WhenSumIs100_ReturnsTrue()
    {
        // Arrange
        var shares = new[] { 50.00m, 30.00m, 20.00m };

        // Act
        var result = _service.ValidateOwnershipShares(shares);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateOwnershipShares_WhenSumIsNot100_ReturnsFalse()
    {
        // Arrange
        var shares = new[] { 50.00m, 30.00m, 15.00m };

        // Act
        var result = _service.ValidateOwnershipShares(shares);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateOwnershipShares_WhenSumIs100WithinTolerance_ReturnsTrue()
    {
        // Arrange - sum is 100.005 which is within default tolerance of 0.01 (difference: 0.005)
        var shares = new[] { 33.335m, 33.335m, 33.335m };

        // Act
        var result = _service.ValidateOwnershipShares(shares);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetOwnershipValidationError_ReturnsCorrectMessage()
    {
        // Arrange
        var shares = new[] { 50.00m, 30.00m, 15.00m };

        // Act
        var error = _service.GetOwnershipValidationError(shares);

        // Assert
        Assert.Contains("95", error);
        Assert.Contains("100", error);
    }
}
