using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Products.Queries.GetProducts;
using Archu.Contracts.Common;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Queries;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<ILogger<GetProductsQueryHandler>> _mockLogger;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<GetProductsQueryHandler>>();
        _handler = new GetProductsQueryHandler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedProducts_WhenProductsExist()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10.99m, RowVersion = new byte[] { 1, 2, 3 } },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 20.99m, RowVersion = new byte[] { 4, 5, 6 } }
        };

        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 5));

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPagedResult_WhenNoProductsExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var products = new List<Product>
        {
            new()
            {
                Id = productId,
                Name = "Test Product",
                Price = 99.99m,
                RowVersion = rowVersion
            }
        };

        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Items.First();
        dto.Id.Should().Be(productId);
        dto.Name.Should().Be("Test Product");
        dto.Price.Should().Be(99.99m);
        dto.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _handler.Handle(query, cts.Token));
    }

    [Fact]
    public async Task Handle_ShouldLogInformationMessages()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m, RowVersion = new byte[] { 1 } }
        };

        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 15));

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving products")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved 1 products")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(1, 10, 5)]
    [InlineData(2, 10, 25)]
    [InlineData(1, 20, 100)]
    [InlineData(5, 10, 50)]
    public async Task Handle_ShouldHandleDifferentPaginationParameters(int pageNumber, int pageSize, int totalCount)
    {
        // Arrange
        var itemsToReturn = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        var products = Enumerable.Range(0, itemsToReturn)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Price = i * 10m,
                RowVersion = new byte[] { (byte)i }
            })
            .ToList();

        _mockRepository
            .Setup(r => r.GetPagedAsync(pageNumber, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalCount));

        var query = new GetProductsQuery(PageNumber: pageNumber, PageSize: pageSize);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(itemsToReturn);
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10m, RowVersion = new byte[] { 1 } }
        };

        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 25));

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultPaginationParameters()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        var query = new GetProductsQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
