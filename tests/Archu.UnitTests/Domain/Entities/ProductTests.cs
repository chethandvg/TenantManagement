using Archu.Domain.Entities;
using Archu.UnitTests.TestHelpers.Builders;
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

    [Fact]
    public void IsOwnedBy_WhenUserIdMatches_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var product = new ProductBuilder()
            .WithOwnerId(userId)
            .Build();

        // Act
        var result = product.IsOwnedBy(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOwnedBy_WhenUserIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var product = new ProductBuilder()
            .WithOwnerId(ownerId)
            .Build();

        // Act
        var result = product.IsOwnedBy(differentUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(99.99)]
    [InlineData(9999.99)]
    public void Product_CanStoreVariousPrices(decimal price)
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithPrice(price)
            .Build();

        // Assert
        product.Price.Should().Be(price);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Short Name")]
    [InlineData("Very Long Product Name That Contains Many Characters And Describes The Product In Detail")]
    public void Product_CanStoreVariousNameLengths(string name)
    {
        // Arrange & Act
        var product = new ProductBuilder()
            .WithName(name)
            .Build();

        // Assert
        product.Name.Should().Be(name);
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

    [Fact]
    public void Product_WhenDeleted_HasDeletionInformation()
    {
        // Arrange
        var deletedAt = DateTime.UtcNow;
        var deletedBy = "TestUser";
        var product = new ProductBuilder()
            .AsDeleted(deletedAt, deletedBy)
            .Build();

        // Assert
        product.IsDeleted.Should().BeTrue();
        product.DeletedAtUtc.Should().Be(deletedAt);
        product.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public void Product_CanBeModified()
    {
        // Arrange
        var modifiedAt = DateTime.UtcNow;
        var modifiedBy = "TestUser";
        var product = new ProductBuilder()
            .WithModifiedInfo(modifiedAt, modifiedBy)
            .Build();

        // Assert
        product.ModifiedAtUtc.Should().Be(modifiedAt);
        product.ModifiedBy.Should().Be(modifiedBy);
    }

    [Fact]
    public void Product_HasCreationInformation()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var createdBy = "TestUser";
        var product = new ProductBuilder()
            .WithCreatedInfo(createdAt, createdBy)
            .Build();

        // Assert
        product.CreatedAtUtc.Should().Be(createdAt);
        product.CreatedBy.Should().Be(createdBy);
    }

    [Fact]
    public void Product_EqualityComparison_BasedOnId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var product1 = new ProductBuilder().WithId(id).Build();
        var product2 = new ProductBuilder().WithId(id).Build();
        var product3 = new ProductBuilder().WithId(Guid.NewGuid()).Build();

        // Act & Assert
        (product1.Id == product2.Id).Should().BeTrue();
        (product1.Id == product3.Id).Should().BeFalse();
    }
}
