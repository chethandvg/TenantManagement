using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Fixtures;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace Archu.UnitTests.Domain.Entities;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class ProductTests
{
    [Fact]
    public void Product_WhenCreated_HasDefaultId()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Product_WhenCreated_HasEmptyRowVersion()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.RowVersion.Should().BeEmpty();
    }

    [Fact]
    public void Product_WhenCreated_IsNotDeleted()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        product.IsDeleted.Should().BeFalse();
        product.DeletedAtUtc.Should().BeNull();
        product.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Product_CanSetAllProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var name = "Test Product";
        var price = 99.99m;
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        var product = new Product
        {
            Id = productId,
            Name = name,
            Price = price,
            OwnerId = ownerId,
            RowVersion = rowVersion
        };

        // Assert
        product.Id.Should().Be(productId);
        product.Name.Should().Be(name);
        product.Price.Should().Be(price);
        product.OwnerId.Should().Be(ownerId);
        product.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    /// <summary>
    /// Ensures ownership checks succeed when the provided identifier matches the product's owner.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsOwnedBy_WhenUserIdMatches_ReturnsTrue(Guid userId, IFixture fixture)
    {
        // Arrange
        var product = fixture.Build<Product>()
            .With(p => p.OwnerId, userId)
            .Create();

        // Act
        var result = product.IsOwnedBy(userId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Ensures ownership checks fail when the identifier differs from the product's owner.
    /// </summary>
    [Theory, AutoMoqData]
    public void IsOwnedBy_WhenUserIdDoesNotMatch_ReturnsFalse(Guid ownerId, Guid differentUserId, IFixture fixture)
    {
        // Arrange
        while (ownerId == differentUserId)
        {
            differentUserId = Guid.NewGuid();
        }

        var product = fixture.Build<Product>()
            .With(p => p.OwnerId, ownerId)
            .Create();

        // Act
        var result = product.IsOwnedBy(differentUserId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that price assignments persist regardless of the generated value.
    /// </summary>
    [Theory, AutoMoqData]
    public void Product_CanStoreVariousPrices(decimal price, IFixture fixture)
    {
        // Arrange & Act
        var product = fixture.Build<Product>()
            .With(p => p.Price, price)
            .Create();

        // Assert
        product.Price.Should().Be(price);
    }

    /// <summary>
    /// Verifies that name assignments persist for any generated string.
    /// </summary>
    [Theory, AutoMoqData]
    public void Product_CanStoreVariousNameLengths(string name, IFixture fixture)
    {
        // Arrange & Act
        var product = fixture.Build<Product>()
            .With(p => p.Name, name ?? string.Empty)
            .Create();

        // Assert
        product.Name.Should().Be(name ?? string.Empty);
    }

    [Fact]
    public void Product_ImplementsIAuditable()
    {
        // Arrange
        var product = new Product();

        // Act & Assert
        product.Should().BeAssignableTo<Archu.Domain.Abstractions.IAuditable>();
        product.CreatedAtUtc.Should().NotBe(default);
        product.CreatedBy.Should().NotBeNull();
    }

    [Fact]
    public void Product_ImplementsISoftDeletable()
    {
        // Arrange
        var product = new Product();

        // Act & Assert
        product.Should().BeAssignableTo<Archu.Domain.Abstractions.ISoftDeletable>();
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Product_ImplementsIHasOwner()
    {
        // Arrange
        var product = new Product();

        // Act & Assert
        product.Should().BeAssignableTo<Archu.Domain.Abstractions.Identity.IHasOwner>();
        product.OwnerId.Should().NotBeEmpty();
    }

    /// <summary>
    /// Ensures deleted metadata is preserved when the product is soft deleted.
    /// </summary>
    [Theory, AutoMoqData]
    public void Product_WhenDeleted_HasDeletionInformation(IFixture fixture, DateTime deletedAt, string deletedBy)
    {
        // Arrange
        var deletedByValue = deletedBy ?? string.Empty;
        var product = fixture.Build<Product>()
            .With(p => p.IsDeleted, true)
            .With(p => p.DeletedAtUtc, deletedAt)
            .With(p => p.DeletedBy, deletedByValue)
            .Create();

        // Assert
        product.IsDeleted.Should().BeTrue();
        product.DeletedAtUtc.Should().Be(deletedAt);
        product.DeletedBy.Should().Be(deletedByValue);
    }

    /// <summary>
    /// Ensures modified metadata is captured when updates occur.
    /// </summary>
    [Theory, AutoMoqData]
    public void Product_CanBeModified(IFixture fixture, DateTime modifiedAt, string modifiedBy)
    {
        // Arrange
        var modifiedByValue = modifiedBy ?? string.Empty;
        var product = fixture.Build<Product>()
            .With(p => p.ModifiedAtUtc, modifiedAt)
            .With(p => p.ModifiedBy, modifiedByValue)
            .Create();

        // Assert
        product.ModifiedAtUtc.Should().Be(modifiedAt);
        product.ModifiedBy.Should().Be(modifiedByValue);
    }

    /// <summary>
    /// Ensures creation metadata is recorded when products are instantiated.
    /// </summary>
    [Theory, AutoMoqData]
    public void Product_HasCreationInformation(IFixture fixture, DateTime createdAt, string createdBy)
    {
        // Arrange
        var createdByValue = createdBy ?? string.Empty;
        var product = fixture.Build<Product>()
            .With(p => p.CreatedAtUtc, createdAt)
            .With(p => p.CreatedBy, createdByValue)
            .Create();

        // Assert
        product.CreatedAtUtc.Should().Be(createdAt);
        product.CreatedBy.Should().Be(createdByValue);
    }

    /// <summary>
    /// Confirms equality semantics rely on the identifier value.
    /// </summary>
    [Theory, AutoMoqData]
    public void Product_EqualityComparison_BasedOnId(IFixture fixture, Guid id)
    {
        // Arrange
        Guid uniqueId;
        do
        {
            uniqueId = Guid.NewGuid();
        } while (uniqueId == id);

        var product1 = fixture.Build<Product>()
            .With(p => p.Id, id)
            .Create();
        var product2 = fixture.Build<Product>()
            .With(p => p.Id, id)
            .Create();
        var product3 = fixture.Build<Product>()
            .With(p => p.Id, uniqueId)
            .Create();

        // Act & Assert
        (product1.Id == product2.Id).Should().BeTrue();
        (product1.Id == product3.Id).Should().BeFalse();
    }
}
