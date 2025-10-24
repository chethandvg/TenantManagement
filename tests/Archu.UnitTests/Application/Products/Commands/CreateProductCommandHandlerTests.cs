using Archu.Application.Abstractions;
using Archu.Application.Products.Commands.CreateProduct;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Fixtures;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class CreateProductCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRequestIsValid_CreatesProductSuccessfully(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        CreateProductCommandHandler handler,
        string productName,
        decimal productPrice)
    {
        // Arrange
        var userId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        Product? capturedProduct = null;
        mockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductCommand(productName, productPrice);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(productName);
        result.Price.Should().Be(productPrice);
        result.Id.Should().NotBeEmpty();

        capturedProduct.Should().NotBeNull();
        capturedProduct!.Name.Should().Be(productName);
        capturedProduct.Price.Should().Be(productPrice);
        capturedProduct.OwnerId.Should().Be(userId);

        mockProductRepository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockUnitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIsNotAuthenticated_ThrowsException(
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        CreateProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Handle_SetsOwnerIdFromCurrentUser(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        CreateProductCommandHandler handler)
    {
        // Arrange
        var expectedOwnerId = Guid.NewGuid();
        mockCurrentUser.Setup(x => x.UserId).Returns(expectedOwnerId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        Product? capturedProduct = null;
        mockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProduct.Should().NotBeNull();
        capturedProduct!.OwnerId.Should().Be(expectedOwnerId);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ReturnsProductDtoWithAllProperties(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        CreateProductCommandHandler handler)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) =>
            {
                p.Id = productId;
                p.RowVersion = rowVersion;
                return p;
            });

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        CreateProductCommandHandler handler)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, ct) =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException();
            })
            .ReturnsAsync((Product p, CancellationToken _) => p);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenSaveFails_ThrowsException(
        [Frozen] Mock<IUnitOfWork> mockUnitOfWork,
        [Frozen] Mock<IProductRepository> mockProductRepository,
        [Frozen] Mock<ICurrentUser> mockCurrentUser,
        CreateProductCommandHandler handler)
    {
        // Arrange
        mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        mockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        mockUnitOfWork.Setup(u => u.Products).Returns(mockProductRepository.Object);
        mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
