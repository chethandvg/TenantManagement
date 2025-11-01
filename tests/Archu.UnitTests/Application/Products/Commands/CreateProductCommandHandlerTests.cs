using System.Collections.Generic;
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

    [Theory, AutoMoqData]
    public async Task Handle_WhenRequestIsValid_CreatesProductSuccessfully(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(productName);
        result.Price.Should().Be(price);
        result.Id.Should().NotBeEmpty();

        fixture.VerifyProductAdded();
        fixture.VerifySaveChangesCalled();
    }

    [Theory, AutoMoqData]
    public async Task Handle_SetsOwnerIdFromCurrentUser(
        string productName,
        decimal price,
        Guid expectedOwnerId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(expectedOwnerId)
            .WithProductRepositoryForAdd();

        Product? capturedProduct = null;
        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProduct.Should().NotBeNull();
        capturedProduct!.OwnerId.Should().Be(expectedOwnerId);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ReturnsProductDtoWithAllProperties(
        string productName,
        decimal price,
        Guid userId,
        Guid productId,
        byte[] rowVersion)
    {
        // Arrange
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

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(productId);
        result.Name.Should().Be(productName);
        result.Price.Should().Be(price);
        result.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    #endregion

    #region Authentication & Authorization Tests

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIsNotAuthenticated_ThrowsException(
        string productName,
        decimal price)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    public static TheoryData<string> InvalidUserIds => new()
    {
        string.Empty,
        "   ",
        "invalid-guid-format",
        "12345",
        "not-a-guid-at-all",
        "GGGGGGGG-GGGG-GGGG-GGGG-GGGGGGGGGGGG"
    };

    [Theory]
    [MemberData(nameof(InvalidUserIds))]
    public async Task Handle_WhenUserIdIsInvalid_ThrowsUnauthorizedAccessException(string invalidUserId)
    {
        await AssertUnauthorizedAccessForInvalidUserAsync(invalidUserId, "Sample Product", 42.5m);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdIsValidGuid_DoesNotThrow(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenCreatingProduct(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "UserId", userId },
            { "ProductName", productName },
            { "{OriginalFormat}", "User {UserId} creating product: {ProductName}" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductCreated(
        string productName,
        decimal price,
        Guid userId,
        Guid productId)
    {
        // Arrange
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

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "ProductId", productId },
            { "UserId", userId },
            { "{OriginalFormat}", "Product created with ID: {ProductId} by User: {UserId}" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsTwoInformationMessages_WhenSuccessful(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyLogCount(LogLevel.Information, 2);
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsError_WhenUserNotAuthenticated(
        string productName,
        decimal price)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        fixture.VerifyStructuredErrorLogged(new Dictionary<string, object?>
        {
            { "Operation", "create products" },
            { "{OriginalFormat}", "Cannot perform {Operation}: User ID not found or invalid" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesUserIdInLogs(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                { "UserId", userId }
            },
            Times.Exactly(2));
    }

    [Theory]
    [InlineAutoMoqData("Special Product Name")]
    [InlineAutoMoqData("Premium Widget")]
    [InlineAutoMoqData("Budget Item")]
    public async Task Handle_IncludesProductNameInLog(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "ProductName", productName }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_IncludesProductIdInSuccessLog(
        string productName,
        decimal price,
        Guid userId,
        Guid productId)
    {
        // Arrange
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

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "ProductId", productId }
        });
    }

    #endregion

    #region Structured Logging Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredUserId(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify structured fields exist in the log
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "UserId", userId },
            { "ProductName", productName }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredProductId(
        string productName,
        decimal price,
        Guid userId,
        Guid productId)
    {
        // Arrange
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

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Verify structured fields exist in the success log
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "ProductId", productId },
            { "UserId", userId }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsErrorWithStructuredOperation(
        string productName,
        decimal price)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithUnauthenticatedUser();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Verify structured operation field
        fixture.VerifyStructuredErrorLogged(new Dictionary<string, object?>
        {
            { "Operation", "create products" }
        });
    }

    #endregion

    #region Cancellation Token Flow Tests

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToAddAsync(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        fixture.VerifyProductAddedWithToken(cancellationToken);
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToSaveChangesAsync(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        fixture.VerifySaveChangesCalledWithToken(cancellationToken);
    }

    #endregion

    #region Error Handling Tests

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        fixture.MockProductRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, ct) =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException();
            })
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(command, cts.Token));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenSaveFails_ThrowsException(
        string productName,
        decimal price,
        Guid userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithAuthenticatedUser(userId)
            .WithProductRepositoryForAdd();

        fixture.MockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    #endregion

    /// <summary>
    /// Provides shared assertions for invalid user scenarios to avoid repetitive arrangements in tests.
    /// Ensures the invalid user configuration, command execution, and exception validation remain consistent.
    /// The helper verifies the handler reads the configured user ID while throwing the expected unauthorized error.
    /// </summary>
    private static async Task AssertUnauthorizedAccessForInvalidUserAsync(string invalidUserId, string productName, decimal price)
    {
        var fixture = new CommandHandlerTestFixture<CreateProductCommandHandler>()
            .WithInvalidUserIdFormat(invalidUserId);

        var handler = fixture.CreateHandler();
        var command = new CreateProductCommand(productName, price);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User must be authenticated to create products");

        fixture.MockCurrentUser.VerifyGet(user => user.UserId, Times.AtLeastOnce());
        fixture.MockCurrentUser.Object.IsAuthenticated.Should().BeTrue();
    }
}
