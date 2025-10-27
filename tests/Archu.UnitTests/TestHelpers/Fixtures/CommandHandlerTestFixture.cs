using Archu.Application.Abstractions;
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
    public Mock<ILogger<THandler>> MockLogger { get; }

    private Guid _authenticatedUserId = Guid.NewGuid();

    public CommandHandlerTestFixture()
    {
        MockUnitOfWork = new Mock<IUnitOfWork>();
        MockProductRepository = new Mock<IProductRepository>();
        MockCurrentUser = new Mock<ICurrentUser>();
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
}
