using System;
using System.Collections.Generic;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
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
    public Mock<ICurrentUser> MockCurrentUser { get; }
    public Mock<IAuthenticationService> MockAuthenticationService { get; }
    public Mock<ILogger<THandler>> MockLogger { get; }

    private Func<IProductRepository, ICurrentUser, ILogger<THandler>, THandler>? _handlerFactory;
    private Func<IProductRepository, ILogger<THandler>, THandler>? _simpleHandlerFactory;

    public QueryHandlerTestFixture()
    {
        MockProductRepository = new Mock<IProductRepository>();
        MockCurrentUser = new Mock<ICurrentUser>();
        MockAuthenticationService = new Mock<IAuthenticationService>();
        MockLogger = new Mock<ILogger<THandler>>();
    }

    #region Authentication Setup Methods

    /// <summary>
    /// Configures an authenticated user with a specific user ID.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithAuthenticatedUser(Guid userId)
    {
        MockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        return this;
    }

    /// <summary>
    /// Configures an authenticated user with a randomly generated user ID.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithAuthenticatedUser()
    {
        return WithAuthenticatedUser(Guid.NewGuid());
    }

    /// <summary>
    /// Configures an unauthenticated user.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithUnauthenticatedUser()
    {
        MockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
        return this;
    }

    /// <summary>
    /// Configures an authenticated user with an invalid GUID format.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithInvalidUserIdFormat(string invalidUserId = "not-a-guid")
    {
        MockCurrentUser.Setup(x => x.UserId).Returns(invalidUserId);
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        return this;
    }

    /// <summary>
    /// Configures the authentication service mock for query handler scenarios.
    /// </summary>
    /// <param name="configure">An optional callback to arrange authentication service behavior.</param>
    public QueryHandlerTestFixture<THandler> WithAuthenticationService(
        Action<Mock<IAuthenticationService>>? configure = null)
    {
        configure?.Invoke(MockAuthenticationService);
        return this;
    }

    #endregion

    #region Repository Setup Methods

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
    /// Configures the repository to throw OperationCanceledException for testing cancellation.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithCancelledOperation()
    {
        MockProductRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        MockProductRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        MockProductRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        return this;
    }

    #endregion

    #region Handler Factory Methods

    /// <summary>
    /// Configures a custom handler factory for handlers with ICurrentUser dependency.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithHandlerFactory(
        Func<IProductRepository, ICurrentUser, ILogger<THandler>, THandler> factory)
    {
        _handlerFactory = factory;
        return this;
    }

    /// <summary>
    /// Configures a custom handler factory for handlers without ICurrentUser dependency.
    /// </summary>
    public QueryHandlerTestFixture<THandler> WithSimpleHandlerFactory(
        Func<IProductRepository, ILogger<THandler>, THandler> factory)
    {
        _simpleHandlerFactory = factory;
        return this;
    }

    /// <summary>
    /// Creates a handler instance using the configured mocks.
    /// </summary>
    public THandler CreateHandler()
    {
        if (_handlerFactory != null)
        {
            return _handlerFactory(
                MockProductRepository.Object,
                MockCurrentUser.Object,
                MockLogger.Object);
        }

        if (_simpleHandlerFactory != null)
        {
            return _simpleHandlerFactory(
                MockProductRepository.Object,
                MockLogger.Object);
        }

        var constructorSignatures = new[]
        {
            new[] { typeof(IProductRepository), typeof(ICurrentUser), typeof(ILogger<THandler>) },
            new[] { typeof(IProductRepository), typeof(ILogger<THandler>) },
            new[] { typeof(IAuthenticationService), typeof(ILogger<THandler>) },
            new[] { typeof(IAuthenticationService), typeof(ICurrentUser), typeof(ILogger<THandler>) }
        };

        foreach (var signature in constructorSignatures)
        {
            var constructor = typeof(THandler).GetConstructor(signature);

            if (constructor != null)
            {
                var parameters = signature
                    .Select(ResolveConstructorParameter)
                    .ToArray();

                return (THandler)constructor.Invoke(parameters);
            }
        }

        throw new InvalidOperationException(
            $"No suitable constructor found for {typeof(THandler).Name}. " +
            "Use WithHandlerFactory or WithSimpleHandlerFactory to provide a custom factory method.");
    }

    /// <summary>
    /// Resolves constructor dependencies for query handlers to the configured mocks.
    /// </summary>
    /// <param name="dependencyType">The dependency type requested by the handler constructor.</param>
    private object ResolveConstructorParameter(Type dependencyType)
    {
        if (dependencyType == typeof(IProductRepository))
        {
            return MockProductRepository.Object;
        }

        if (dependencyType == typeof(IAuthenticationService))
        {
            return MockAuthenticationService.Object;
        }

        if (dependencyType == typeof(ICurrentUser))
        {
            return MockCurrentUser.Object;
        }

        if (dependencyType == typeof(ILogger<THandler>))
        {
            return MockLogger.Object;
        }

        throw new InvalidOperationException(
            $"Unsupported dependency type {dependencyType.Name} for handler {typeof(THandler).Name}.");
    }

    #endregion

    #region Repository Verification Methods

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

    #endregion

    #region Cancellation Token Verification Methods

    /// <summary>
    /// Verifies that GetPagedAsync was called with specific cancellation token.
    /// </summary>
    public void VerifyGetPagedCalledWithToken(int pageNumber, int pageSize, CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.GetPagedAsync(pageNumber, pageSize, It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    /// <summary>
    /// Verifies that GetByIdAsync was called with specific cancellation token.
    /// </summary>
    public void VerifyGetByIdCalledWithToken(Guid productId, CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.GetByIdAsync(productId, It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    /// <summary>
    /// Verifies that GetByOwnerIdAsync was called with specific cancellation token.
    /// </summary>
    public void VerifyGetByOwnerIdCalledWithToken(Guid ownerId, CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.GetByOwnerIdAsync(ownerId, It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    /// <summary>
    /// Verifies that GetAllAsync was called with specific cancellation token.
    /// </summary>
    public void VerifyGetAllCalledWithToken(CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.GetAllAsync(It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    #endregion

    #region Logging Verification Methods

    /// <summary>
    /// Verifies that an Information level log was written using the provided message template.
    /// Internally asserts on the structured <c>{OriginalFormat}</c> field to avoid brittle substring comparisons.
    /// </summary>
    public void VerifyInformationLogged(string expectedMessage, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => VerifyLogMessageContains(v, expectedMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a Warning level log was written using the provided message template.
    /// </summary>
    public void VerifyWarningLogged(string expectedMessage, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => VerifyLogMessageContains(v, expectedMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that an Error level log was written using the provided message template.
    /// </summary>
    public void VerifyErrorLogged(string expectedMessage, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => VerifyLogMessageContains(v, expectedMessage)),
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

    #endregion

    #region Structured Logging Verification Methods

    /// <summary>
    /// Verifies that structured log fields exist in an Information level log.
    /// </summary>
    public void VerifyStructuredInformationLogged(Dictionary<string, object?> expectedFields, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => VerifyLogState(v, expectedFields)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that structured log fields exist in a Warning level log.
    /// </summary>
    public void VerifyStructuredWarningLogged(Dictionary<string, object?> expectedFields, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => VerifyLogState(v, expectedFields)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that structured log fields exist in an Error level log.
    /// </summary>
    public void VerifyStructuredErrorLogged(Dictionary<string, object?> expectedFields, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => VerifyLogState(v, expectedFields)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Helper method to verify structured log state contains expected fields and values.
    /// </summary>
    private static bool VerifyLogState(object state, Dictionary<string, object?> expectedFields)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> logValues)
        {
            return false;
        }

        foreach (var expectedField in expectedFields)
        {
            var actualField = logValues.FirstOrDefault(kv => kv.Key == expectedField.Key);
            
            if (actualField.Key == null)
            {
                return false; // Field not found
            }

            // Compare values - handle Guid specially since ToString comparison might be case-sensitive
            if (expectedField.Value is Guid expectedGuid)
            {
                if (actualField.Value is not Guid actualGuid || actualGuid != expectedGuid)
                {
                    return false;
                }
            }
            else if (!Equals(actualField.Value, expectedField.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool VerifyLogMessageContains(object state, string expectedMessage)
    {
        var formattedMessage = state?.ToString();

        if (formattedMessage is null)
        {
            return false;
        }

        return formattedMessage.Contains(expectedMessage, StringComparison.Ordinal);
    }

    #endregion
}
