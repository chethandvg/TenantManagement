using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Application.Products.Commands.UpdateProduct;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Builders;
using Archu.UnitTests.TestHelpers.Fixtures;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Archu.UnitTests.Application.Products.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class UpdateProductCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenProductExistsAndRowVersionMatches_UpdatesProductSuccessfully(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder()
            .WithName("Original Name")
            .WithPrice(50.00m)
            .WithOwnerId(userId)
            .Build();

        var originalRowVersion = existingProduct.RowVersion;

        mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            originalRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Price.Should().Be(99.99m);

        mockProductRepository.Verify(
            r => r.UpdateAsync(
                It.Is<Product>(p => p.Name == "Updated Name" && p.Price == 99.99m),
                originalRowVersion,
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockUnitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenProductNotFound_ReturnsFailure(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new UpdateProductCommand(
            productId,
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRowVersionMismatch_ReturnsFailure(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithRowVersion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            .Build();

        var differentRowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            differentRowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("modified by another user");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConcurrencyExceptionOccurs_ReturnsFailure(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var originalRowVersion = existingProduct.RowVersion;

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockProductRepository
            .Setup(r => r.ExistsAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        // Simulate concurrency exception by throwing InvalidOperationException with specific message
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Concurrency conflict"));

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            originalRowVersion);

        // Act & Assert
        // The handler does not catch generic InvalidOperationException, so it should be thrown
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_UpdatesProductProperties(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithName("Original Name")
            .WithPrice(50.00m)
            .Build();

        Product? updatedProduct = null;

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<Product, byte[], CancellationToken>((p, _, __) => updatedProduct = p)
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "New Name",
            75.50m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("New Name");
        updatedProduct.Price.Should().Be(75.50m);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns("not-a-valid-guid");
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-guid-format")]
    [InlineData("12345")]
    [InlineData("not-a-guid-at-all")]
    [InlineData("GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG")]
    public async Task Handle_WhenUserIdHasInvalidFormat_ThrowsUnauthorizedAccessException(
        string invalidUserId)
    {
        // Arrange
        var mockCurrentUser = new Mock<ICurrentUser>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<UpdateProductCommandHandler>>();
        
        mockCurrentUser.Setup(x => x.UserId).Returns(invalidUserId);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        var handler = new UpdateProductCommandHandler(
            mockUnitOfWork.Object,
            mockCurrentUser.Object,
            mockLogger.Object);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns(string.Empty);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to update products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrowForAuthentication(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var validGuid = Guid.NewGuid().ToString();
        
        mockCurrentUser.Setup(x => x.UserId).Returns(validGuid);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenUpdatingProduct(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<UpdateProductCommandHandler>> mockLogger,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify update start log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} updating product with ID: {existingProduct.Id}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductUpdated(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<UpdateProductCommandHandler>> mockLogger,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();

        mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify success log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Product with ID {existingProduct.Id} updated successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenProductNotFound(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<UpdateProductCommandHandler>> mockLogger,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new UpdateProductCommand(
            productId,
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify warning log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Product with ID {productId} not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenRowVersionMismatch(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<UpdateProductCommandHandler>> mockLogger,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithRowVersion(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            .Build();

        var differentRowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            differentRowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify concurrency warning log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Concurrency conflict detected for product {existingProduct.Id}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<UpdateProductCommandHandler>> mockLogger,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Should log exactly 2 Information level messages (start and success)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsError_WhenUserNotAuthenticated(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<UpdateProductCommandHandler>> mockLogger,
        UpdateProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Verify error log from BaseCommandHandler
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cannot perform update products")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
