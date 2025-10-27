using Archu.Application.Abstractions;
using Archu.Application.Products.Commands.CreateProduct;
using Archu.Contracts.Products;
using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Builders;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Products.Commands.Examples;

/// <summary>
/// Example tests demonstrating the use of CommandHandlerTestFixture to reduce duplicate setup code.
/// This file shows the recommended pattern for writing new command handler tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
[Trait("Example", "Refactored")]
public class CreateProductCommandHandlerRefactoredExamples
{
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

        // Verify using fixture helper methods
        fixture.VerifyProductAdded();
        fixture.VerifySaveChangesCalled();
        fixture.VerifyLogCount(LogLevel.Information, 2); // Start + Success logs
    }

    [Fact]
    public async Task Handle_LogsExpectedMessages()
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

        var command = new CreateProductCommand("Special Product", 199.99m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify specific log messages
        fixture.VerifyInformationLogged($"User {userId} creating product");
        fixture.VerifyInformationLogged("Product created with ID");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsException()
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

        // Verify error was logged
        fixture.VerifyErrorLogged("Cannot perform create products");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsInvalid_ThrowsException()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithInvalidUserIdFormat("not-a-guid");

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
    public async Task Handle_SetsOwnerIdCorrectly()
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
}

/// <summary>
/// Example tests demonstrating the use of CommandHandlerTestFixture for Update operations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
[Trait("Example", "Refactored")]
public class UpdateProductCommandHandlerRefactoredExamples
{
    [Fact]
    public async Task Handle_WhenProductExists_UpdatesSuccessfully()
    {
        // Arrange
        var existingProduct = new ProductBuilder()
            .WithName("Original Name")
            .WithPrice(50.00m)
            .Build();

        var fixture = new CommandHandlerTestFixture<Archu.Application.Products.Commands.UpdateProduct.UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new Archu.Application.Products.Commands.UpdateProduct.UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new Archu.Application.Products.Commands.UpdateProduct.UpdateProductCommand(
            existingProduct.Id,
            "Updated Name",
            99.99m,
            existingProduct.RowVersion);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        result.Value.Price.Should().Be(99.99m);

        // Verify using fixture
        fixture.VerifyProductFetched(existingProduct.Id);
        fixture.VerifyProductUpdated();
        fixture.VerifySaveChangesCalled();
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fixture = new CommandHandlerTestFixture<Archu.Application.Products.Commands.UpdateProduct.UpdateProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithProductNotFound(productId);

        var handler = new Archu.Application.Products.Commands.UpdateProduct.UpdateProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new Archu.Application.Products.Commands.UpdateProduct.UpdateProductCommand(
            productId,
            "Updated Name",
            99.99m,
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");

        // Verify warning was logged
        fixture.VerifyWarningLogged($"Product with ID {productId} not found");
    }
}

/// <summary>
/// Example tests demonstrating the use of CommandHandlerTestFixture for Delete operations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
[Trait("Example", "Refactored")]
public class DeleteProductCommandHandlerRefactoredExamples
{
    [Fact]
    public async Task Handle_WhenProductExists_DeletesSuccessfully()
    {
        // Arrange
        var existingProduct = new ProductBuilder().Build();
        var fixture = new CommandHandlerTestFixture<Archu.Application.Products.Commands.DeleteProduct.DeleteProductCommandHandler>()
            .WithAuthenticatedUser()
            .WithExistingProduct(existingProduct);

        var handler = new Archu.Application.Products.Commands.DeleteProduct.DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new Archu.Application.Products.Commands.DeleteProduct.DeleteProductCommand(existingProduct.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify using fixture
        fixture.VerifyProductFetched(existingProduct.Id);
        fixture.VerifyProductDeleted();
        fixture.VerifySaveChangesCalled();
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Fact]
    public async Task Handle_LogsUserIdInAllMessages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingProduct = new ProductBuilder().Build();
        var fixture = new CommandHandlerTestFixture<Archu.Application.Products.Commands.DeleteProduct.DeleteProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithExistingProduct(existingProduct);

        var handler = new Archu.Application.Products.Commands.DeleteProduct.DeleteProductCommandHandler(
            fixture.MockUnitOfWork.Object,
            fixture.MockCurrentUser.Object,
            fixture.MockLogger.Object);

        var command = new Archu.Application.Products.Commands.DeleteProduct.DeleteProductCommand(existingProduct.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"User {userId} deleting product");
        fixture.VerifyInformationLogged($"Product with ID {existingProduct.Id} deleted successfully");
    }
}
