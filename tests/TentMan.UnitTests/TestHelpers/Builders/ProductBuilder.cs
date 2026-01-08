using TentMan.Domain.Entities;

namespace TentMan.UnitTests.TestHelpers.Builders;

/// <summary>
/// Builder pattern for creating Product test data with sensible defaults.
/// Provides a fluent API for constructing Product entities in tests.
/// </summary>
/// <example>
/// Usage:
/// <code>
/// // Simple product with defaults
/// var product = new ProductBuilder().Build();
/// 
/// // Custom product
/// var expensiveProduct = new ProductBuilder()
///     .WithName("Premium Product")
///     .WithPrice(999.99m)
///     .WithOwnerId(userId)
///     .Build();
/// 
/// // Multiple products
/// var products = ProductBuilder.CreateMany(5);
/// </code>
/// </example>
public class ProductBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Product";
    private decimal _price = 99.99m;
    private byte[] _rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
    private Guid _ownerId = Guid.NewGuid();
    private bool _isDeleted = false;
    private DateTime _createdAtUtc = DateTime.UtcNow;
    private string? _createdBy = "TestUser";
    private DateTime? _modifiedAtUtc = null;
    private string? _modifiedBy = null;
    private DateTime? _deletedAtUtc = null;
    private string? _deletedBy = null;

    public ProductBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public ProductBuilder WithRowVersion(byte[] rowVersion)
    {
        _rowVersion = rowVersion;
        return this;
    }

    public ProductBuilder WithOwnerId(Guid ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public ProductBuilder AsDeleted(DateTime? deletedAtUtc = null, string? deletedBy = null)
    {
        _isDeleted = true;
        _deletedAtUtc = deletedAtUtc ?? DateTime.UtcNow;
        _deletedBy = deletedBy ?? "TestUser";
        return this;
    }

    public ProductBuilder WithCreatedInfo(DateTime createdAtUtc, string? createdBy)
    {
        _createdAtUtc = createdAtUtc;
        _createdBy = createdBy;
        return this;
    }

    public ProductBuilder WithModifiedInfo(DateTime modifiedAtUtc, string? modifiedBy)
    {
        _modifiedAtUtc = modifiedAtUtc;
        _modifiedBy = modifiedBy;
        return this;
    }

    /// <summary>
    /// Builds a Product entity with the configured values.
    /// </summary>
    public Product Build()
    {
        return new Product
        {
            Id = _id,
            Name = _name,
            Price = _price,
            RowVersion = _rowVersion,
            OwnerId = _ownerId,
            IsDeleted = _isDeleted,
            CreatedAtUtc = _createdAtUtc,
            CreatedBy = _createdBy,
            ModifiedAtUtc = _modifiedAtUtc,
            ModifiedBy = _modifiedBy,
            DeletedAtUtc = _deletedAtUtc,
            DeletedBy = _deletedBy
        };
    }

    /// <summary>
    /// Creates a list of products with incremental names and prices.
    /// </summary>
    /// <param name="count">Number of products to create.</param>
    /// <returns>List of products with unique IDs and incremental values.</returns>
    public static List<Product> CreateMany(int count = 3)
    {
        var products = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            products.Add(new ProductBuilder()
                .WithName($"Product {i + 1}")
                .WithPrice(Math.Round((i + 1) * 10.99m, 2))
                .Build());
        }
        return products;
    }

    /// <summary>
    /// Creates a list of products owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The owner's user ID.</param>
    /// <param name="count">Number of products to create.</param>
    /// <returns>List of products owned by the specified user.</returns>
    public static List<Product> CreateManyForOwner(Guid ownerId, int count = 3)
    {
        var products = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            products.Add(new ProductBuilder()
                .WithOwnerId(ownerId)
                .WithName($"Product {i + 1}")
                .WithPrice(Math.Round((i + 1) * 10.99m, 2))
                .Build());
        }
        return products;
    }
}
