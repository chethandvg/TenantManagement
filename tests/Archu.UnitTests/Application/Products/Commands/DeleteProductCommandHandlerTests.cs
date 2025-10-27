using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Application.Products.Commands.DeleteProduct;
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
public class DeleteProductCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenProductExists_DeletesProductSuccessfully(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder()
            .WithOwnerId(userId)
            .Build();

        mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        mockProductRepository.Verify(
            r => r.DeleteAsync(existingProduct, It.IsAny<CancellationToken>()),
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
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new DeleteProductCommand(productId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_CallsDeleteAsyncWithCorrectProduct(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        Product? deletedProduct = null;

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => deletedProduct = p)
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        deletedProduct.Should().NotBeNull();
        deletedProduct.Should().BeSameAs(existingProduct);
    }

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenDeleteFails_ThrowsException(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        mockProductRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_DoesNotCallDeleteAsync_WhenProductNotFound(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new DeleteProductCommand(productId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mockProductRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns("not-a-valid-guid");
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
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
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<DeleteProductCommandHandler>>();
        
        mockCurrentUser.Setup(x => x.UserId).Returns(invalidUserId);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        var handler = new DeleteProductCommandHandler(
            mockUnitOfWork.Object,
            mockCurrentUser.Object,
            mockLogger.Object);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns(string.Empty);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to delete products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrowForAuthentication(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        DeleteProductCommandHandler handler)
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
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenDeletingProduct(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
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
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify delete start log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} deleting product with ID: {existingProduct.Id}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductDeleted(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
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
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify success log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Product with ID {existingProduct.Id} deleted successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWarning_WhenProductNotFound(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new DeleteProductCommand(productId);

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
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
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
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

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
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Verify error log from BaseCommandHandler
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cannot perform delete products")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesUserIdInLogs(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
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
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Both log messages should include user ID
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(userId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesProductIdInAllLogs(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        [Frozen] Mock<ILogger<DeleteProductCommandHandler>> mockLogger,
        DeleteProductCommandHandler handler)
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
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - All log messages should include product ID
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(existingProduct.Id.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
