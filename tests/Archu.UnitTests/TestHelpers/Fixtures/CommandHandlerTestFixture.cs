using System;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Authentication;
using Archu.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Archu.UnitTests.TestHelpers.Fixtures;

/// <summary>
/// Test fixture for command handler tests that provides common mock setup and helper methods.
/// Reduces duplicate setup code and improves test maintainability.
/// </summary>
/// <typeparam name="THandler">The type of command handler being tested.</typeparam>
/// <example>
/// Usage:
/// <code>
/// var fixture = new CommandHandlerTestFixture&lt;CreateProductCommandHandler&gt;()
///     .WithAuthenticatedUser(userId)
///     .WithProductRepository();
/// 
/// var handler = fixture.CreateHandler();
/// var result = await handler.Handle(command, CancellationToken.None);
/// 
/// fixture.VerifyInformationLogged("Product created");
/// </code>
/// </example>
public class CommandHandlerTestFixture<THandler> where THandler : class
{
    public Mock<IUnitOfWork> MockUnitOfWork { get; }
    public Mock<IProductRepository> MockProductRepository { get; }
    public Mock<ICurrentUser> MockCurrentUser { get; }
    public Mock<IAuthenticationService> MockAuthenticationService { get; }
    public Mock<ILogger<THandler>> MockLogger { get; }

    private Guid _authenticatedUserId = Guid.NewGuid();
    private Func<IUnitOfWork, ICurrentUser, ILogger<THandler>, THandler>? _handlerFactory;

    public CommandHandlerTestFixture()
    {
        MockUnitOfWork = new Mock<IUnitOfWork>();
        MockProductRepository = new Mock<IProductRepository>();
        MockCurrentUser = new Mock<ICurrentUser>();
        MockAuthenticationService = new Mock<IAuthenticationService>();
        MockLogger = new Mock<ILogger<THandler>>();
    }

    /// <summary>
    /// Configures an authenticated user with a specific user ID.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithAuthenticatedUser(Guid userId)
    {
        _authenticatedUserId = userId;

        MockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        return this;
    }

    /// <summary>
    /// Configures an authenticated user with a randomly generated user ID.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithAuthenticatedUser()
    {
        return WithAuthenticatedUser(Guid.NewGuid());
    }

    /// <summary>
    /// Configures an unauthenticated user.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithUnauthenticatedUser()
    {

        MockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);

        return this;
    }

    /// <summary>
    /// Configures an authenticated user with an invalid GUID format.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithInvalidUserIdFormat(string invalidUserId = "not-a-guid")
    {
        MockCurrentUser.Setup(x => x.UserId).Returns(invalidUserId);
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        return this;
    }

    /// <summary>
    /// Configures the authentication service mock for scenarios that require authentication workflows.
    /// </summary>
    /// <param name="configure">An optional callback to arrange the authentication service behavior.</param>
    public CommandHandlerTestFixture<THandler> WithAuthenticationService(
        Action<Mock<IAuthenticationService>>? configure = null)
    {
        configure?.Invoke(MockAuthenticationService);
        return this;
    }

    /// <summary>
    /// Configures the product repository with standard setup for add operations.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithProductRepositoryForAdd()
    {
        MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        MockUnitOfWork.Setup(u => u.Products).Returns(MockProductRepository.Object);
        MockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return this;
    }

    /// <summary>
    /// Configures the product repository with a specific product for get operations.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithExistingProduct(Product product)
    {
        MockProductRepository
            .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        MockProductRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MockProductRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MockUnitOfWork.Setup(u => u.Products).Returns(MockProductRepository.Object);
        MockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return this;
    }

    /// <summary>
    /// Configures the product repository to return null for a specific product ID.
    /// </summary>
    public CommandHandlerTestFixture<THandler> WithProductNotFound(Guid productId)
    {
        MockProductRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        MockUnitOfWork.Setup(u => u.Products).Returns(MockProductRepository.Object);

        return this;
    }

    /// <summary>
    /// Configures a custom handler factory for creating handler instances.
    /// This is useful when the handler has a non-standard constructor signature.
    /// </summary>
    /// <param name="factory">A factory function that creates handler instances.</param>
    /// <returns>The fixture for method chaining.</returns>
    /// <example>
    /// <code>
    /// fixture.WithHandlerFactory((unitOfWork, currentUser, logger) => 
    ///     new CustomHandler(unitOfWork, currentUser, logger, additionalDependency));
    /// </code>
    /// </example>
    public CommandHandlerTestFixture<THandler> WithHandlerFactory(
        Func<IUnitOfWork, ICurrentUser, ILogger<THandler>, THandler> factory)
    {
        _handlerFactory = factory;
        return this;
    }

    /// <summary>
    /// Creates a handler instance using the configured mocks.
    /// If a custom factory was configured via WithHandlerFactory, it will be used.
    /// Otherwise, attempts to use one of the supported constructor signatures:
    /// (IUnitOfWork, ICurrentUser, ILogger&lt;THandler&gt;), (IAuthenticationService, ILogger&lt;THandler&gt;),
    /// (IAuthenticationService, ICurrentUser, ILogger&lt;THandler&gt;), (ICurrentUser, ILogger&lt;THandler&gt;),
    /// (IUnitOfWork, ILogger&lt;THandler&gt;), or (ILogger).
    /// This ensures handlers that rely on either generic or non-generic loggers can be resolved without
    /// additional test setup.
    /// </summary>
    /// <returns>A new handler instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when handler cannot be created and no custom factory was provided.</exception>
    /// <example>
    /// <code>
    /// // Standard usage
    /// var handler = fixture
    ///     .WithAuthenticatedUser()
    ///     .WithProductRepositoryForAdd()
    ///     .CreateHandler();
    /// 
    /// // With custom factory
    /// var handler = fixture
    ///     .WithAuthenticatedUser()
    ///     .WithHandlerFactory((uow, user, logger) => new MyHandler(uow, user, logger))
    ///     .CreateHandler();
    /// </code>
    /// </example>
    public THandler CreateHandler()
    {
        if (_handlerFactory != null)
        {
            return _handlerFactory(
                MockUnitOfWork.Object,
                MockCurrentUser.Object,
                MockLogger.Object);
        }

        var constructorSignatures = new[]
        {
            new[] { typeof(IUnitOfWork), typeof(ICurrentUser), typeof(ILogger<THandler>) },
            new[] { typeof(IAuthenticationService), typeof(ILogger<THandler>) },
            new[] { typeof(IAuthenticationService), typeof(ICurrentUser), typeof(ILogger<THandler>) },
            new[] { typeof(ICurrentUser), typeof(ILogger<THandler>) },
            new[] { typeof(IUnitOfWork), typeof(ILogger<THandler>) },
            new[] { typeof(ILogger) }
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
            "Use WithHandlerFactory to provide a custom factory method.");
    }

    /// <summary>
    /// Resolves constructor dependencies to their configured mocks when instantiating handlers.
    /// </summary>
    /// <param name="dependencyType">The dependency type requested by the handler constructor.</param>
    private object ResolveConstructorParameter(Type dependencyType)
    {
        if (dependencyType == typeof(IUnitOfWork))
        {
            return MockUnitOfWork.Object;
        }

        if (dependencyType == typeof(IAuthenticationService))
        {
            return MockAuthenticationService.Object;
        }

        if (dependencyType == typeof(ICurrentUser))
        {
            return MockCurrentUser.Object;
        }

        if (dependencyType == typeof(ILogger<THandler>) || dependencyType == typeof(ILogger))
        {
            return MockLogger.Object;
        }

        throw new InvalidOperationException(
            $"Unsupported dependency type {dependencyType.Name} for handler {typeof(THandler).Name}.");
    }

    /// <summary>
    /// Gets the configured authenticated user ID.
    /// </summary>
    public Guid AuthenticatedUserId => _authenticatedUserId;

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
    /// Verifies that structured log fields exist in an Information level log.
    /// </summary>
    /// <param name="expectedFields">Dictionary of field names and their expected values.</param>
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
    /// Verifies that structured log fields exist in a Warning level log.
    /// </summary>
    /// <param name="expectedFields">Dictionary of field names and their expected values.</param>
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
    /// Verifies that an Error level log was written with the specified message and exception payload.
    /// </summary>
    /// <param name="expectedMessage">The log message expected to be present in the log entry.</param>
    /// <param name="expectedException">The exception instance expected to be associated with the log entry.</param>
    /// <param name="times">Optional Moq <see cref="Times"/> constraint describing how often the log should appear.</param>
    public void VerifyErrorLogged(string expectedMessage, Exception expectedException, Times? times = null)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.Is<Exception?>(ex => ReferenceEquals(ex, expectedException)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that structured log fields exist in an Error level log.
    /// </summary>
    /// <param name="expectedFields">Dictionary of field names and their expected values.</param>
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

    /// <summary>
    /// Verifies that SaveChangesAsync was called on the unit of work.
    /// </summary>
    public void VerifySaveChangesCalled(Times? times = null)
    {
        MockUnitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a product was added to the repository.
    /// </summary>
    public void VerifyProductAdded(Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a product was updated in the repository.
    /// </summary>
    public void VerifyProductUpdated(Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a product was deleted from the repository.
    /// </summary>
    public void VerifyProductDeleted(Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that GetByIdAsync was called for a specific product ID.
    /// </summary>
    public void VerifyProductFetched(Guid productId, Times? times = null)
    {
        MockProductRepository.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            times ?? Times.Once());
    }

    /// <summary>
    /// Verifies that a repository method was called with a specific cancellation token.
    /// </summary>
    /// <param name="expectedToken">The expected cancellation token.</param>
    public void VerifyProductAddedWithToken(CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    /// <summary>
    /// Verifies that SaveChangesAsync was called with a specific cancellation token.
    /// </summary>
    /// <param name="expectedToken">The expected cancellation token.</param>
    public void VerifySaveChangesCalledWithToken(CancellationToken expectedToken)
    {
        MockUnitOfWork.Verify(
            u => u.SaveChangesAsync(It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    /// <summary>
    /// Verifies that UpdateAsync was called with a specific cancellation token.
    /// </summary>
    /// <param name="expectedToken">The expected cancellation token.</param>
    public void VerifyProductUpdatedWithToken(CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }

    /// <summary>
    /// Verifies that GetByIdAsync was called with a specific cancellation token.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="expectedToken">The expected cancellation token.</param>
    public void VerifyProductFetchedWithToken(Guid productId, CancellationToken expectedToken)
    {
        MockProductRepository.Verify(
            r => r.GetByIdAsync(productId, It.Is<CancellationToken>(t => t == expectedToken)),
            Times.Once());
    }
}
