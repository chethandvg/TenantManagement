using Archu.Application.Products.Commands.CreateProduct;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class CreateProductCommandHandlerTests
{
    #region Happy Path Tests

    [Fact]
    public async Task Handle_WhenRequestIsValid_CreatesProductSuccessfully()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.Id.Should().NotBeEmpty();

        fixture.VerifyProductAdded();
        fixture.VerifySaveChangesCalled();
    }

    [Fact]
    public async Task Handle_SetsOwnerIdFromCurrentUser()
    {
        // Arrange
        var expectedOwnerId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(expectedOwnerId)
            .WithProductRepositoryForAdd();

        Product? capturedProduct = null;
        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProduct.Should().NotBeNull();
        capturedProduct!.OwnerId.Should().Be(expectedOwnerId);
    }

    [Fact]
    public async Task Handle_ReturnsProductDtoWithAllProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) =>
            {
                p.Id = productId;
                p.RowVersion = rowVersion;
                return p;
            });

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    #endregion

    #region Authentication & Authorization Tests

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ThrowsException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUserIdIsInvalidGuid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithInvalidUserIdFormat("not-a-valid-guid");

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to create products");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-guid-format")]
    [InlineData("12345")]
    [InlineData("not-a-guid-at-all")]
    [InlineData("GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG")]
    public async Task Handle_WhenUserIdHasInvalidFormat_ThrowsUnauthorizedAccessException(string invalidUserId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to create products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmptyString_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithInvalidUserIdFormat(string.Empty);

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to create products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsWhitespace_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithInvalidUserIdFormat("   ");

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to create products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrow()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Logging Verification Tests

    [Fact]
    public async Task Handle_LogsInformation_WhenCreatingProduct()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"User {userId} creating product: Test Product");
    }

    [Fact]
    public async Task Handle_LogsInformation_AfterProductCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) =>
            {
                p.Id = productId;
                return p;
            });

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Product created with ID: {productId}");
    }

    [Fact]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Fact]
    public async Task Handle_LogsError_WhenUserNotAuthenticated()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        fixture.VerifyErrorLogged("Cannot perform create products");
    }

    [Fact]
    public async Task Handle_IncludesUserIdInLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged(userId.ToString(), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_IncludesProductNameInLog()
    {
        // Arrange
        var productName = "Special Product Name";
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand(productName, 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged(productName);
    }

    [Fact]
    public async Task Handle_IncludesProductIdInSuccessLog()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) =>
            {
                p.Id = productId;
                return p;
            });

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Product created with ID: {productId}");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, ct) =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException();
            })
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Fact]
    public async Task Handle_WhenSaveFails_ThrowsException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductRepositoryForAdd();

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var handler = new CreateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    #endregion
}
