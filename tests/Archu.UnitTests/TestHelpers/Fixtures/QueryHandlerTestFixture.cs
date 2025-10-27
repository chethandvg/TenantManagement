using Archu.Application.Abstractions;
using Archu.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Archu.UnitTests.TestHelpers.Fixtures;

/// <summary>
/// Test fixture for query handler tests that provides common mock setup and helper methods.
/// Reduces duplicate setup code and improves test maintainability for read operations.
/// </summary>
/// <typeparam name="THandler">The type of query handler being tested.</typeparam>
/// <example>
/// Usage:
/// <code>
/// var fixture = new QueryHandlerTestFixture&lt;GetProductsQueryHandler&gt;()
///     .WithProducts(productList)
///     .WithPagedProducts(100, 10);
/// 
/// var handler = fixture.CreateHandler();
/// var result = await handler.Handle(query, CancellationToken.None);
/// 
/// fixture.VerifyProductsFetched();
/// </code>
/// </example>
public class QueryHandlerTestFixture<THandler> where THandler : class
{
    public Mock<IProductRepository> MockProductRepository { get; }
    public Mock<ILogger<THandler>> MockLogger { get; }

    public QueryHandlerTestFixture()
    {
        MockProductRepository = new Mock<IProductRepository>();
        MockLogger = new Mock<ILogger<THandler>>();
    }

    /// <summary>
    /// Configures the repository to return a specific list of products.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithProducts(List<Product> products)
    {
        MockProductRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        return this;
    }

    /// <summary>
    /// Configures the repository to return an empty product list.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithEmptyProductList()
    {
        MockProductRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        MockProductRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        return this;
    }

    /// <summary>
    /// Configures the repository for paged product queries.
    /// </summary>
    /// <param name="totalCount">Total number of products in the database.</param>
    /// <param name="pageSize">Number of products per page.</param>
    /// <param name="currentPage">Current page number (1-based).</param>
    public QueryHandlerTestFixture<THandler> WithPagedProducts(int totalCount, int pageSize, int currentPage = 1)
    {
        // Calculate how many products to return for this page
        var skip = (currentPage - 1) * pageSize;
        var take = Math.Max(0, Math.Min(pageSize, totalCount - skip));

        var products = take > 0
            ? Enumerable.Range(skip, take)
                .Select(i => new Product
                {
                    Id = Guid.NewGuid(),
                    Name = $"Product {i + 1}",
                    Price = (i + 1) * 10.00m,
                    RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
                })
                .ToList()
            : new List<Product>();

        MockProductRepository
            .Setup(r => r.GetPagedAsync(currentPage, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalCount));

        return this;
    }

    /// <summary>
    /// Configures the repository to return a specific product by ID.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithProduct(Product product)
    {
        MockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        return this;
    }

    /// <summary>
    /// Configures the repository to return null for a specific product ID.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithProductNotFound(Guid productId)
    {
        MockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        return this;
    }

    /// <summary>
    /// Configures the repository to return products for a specific owner.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithProductsForOwner(Guid ownerId, List<Product> products)
    {
        MockProductRepository
            .Setup(r => r.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        return this;
    }

    /// <summary>
    /// Configures the repository to return an empty list for a specific owner.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithNoProductsForOwner(Guid ownerId)
    {
        MockProductRepository
            .Setup(r => r.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        return this;
    }

    /// <summary>
    /// Verifies that GetAllAsync was called on the repository.
    /// </summary>
    public void VerifyGetAllCalled(Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that GetPagedAsync was called on the repository.
    /// </summary>
    public void VerifyGetPagedCalled(int? pageNumber = null, int? pageSize = null, Times? times = null)
    {
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            MockProductRepository.Verify(
                r => r.GetPagedAsync(pageNumber.Value, pageSize.Value, It.IsAny<CancellationToken>()),
                times ?? Times.Once());
        }
        else
        {
            MockProductRepository.Verify(
                r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                times ?? Times.Once());
        }
    }

    /// <summary>
    /// Verifies that GetByIdAsync was called for a specific product ID.
    /// </summary>
    public void VerifyGetByIdCalled(Guid productId, Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that GetByOwnerIdAsync was called for a specific owner.
    /// </summary>
    public void VerifyGetByOwnerIdCalled(Guid ownerId, Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that an Information level log was written containing the specified message.
    /// </summary>
    public void VerifyInformationLogged(string expectedMessage, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a Warning level log was written containing the specified message.
    /// </summary>
    public void VerifyWarningLogged(string expectedMessage, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that an Error level log was written containing the specified message.
    /// </summary>
    public void VerifyErrorLogged(string expectedMessage, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that exactly N log messages were written at the specified log level.
    /// </summary>
    public void VerifyLogCount(LogLevel logLevel, int expectedCount)
    {
        MockLogger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(expectedCount));
    }
}
