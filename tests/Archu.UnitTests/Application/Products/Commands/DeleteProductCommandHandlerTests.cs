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
}
