using Archu.Domain.Entities;
using Archu.Infrastructure.Persistence;

namespace Archu.IntegrationTests.TestHelpers;

/// <summary>
/// Helper class for seeding test data into the in-memory database.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Creates and seeds a test user.
    /// </summary>
    public static async Task<Guid> SeedUserAsync(
        ApplicationDbContext context,
        string? email = null,
        string? username = null,
        bool emailConfirmed = true)
    {
        var userId = Guid.NewGuid();
        var user = new Domain.Entities.Identity.ApplicationUser
        {
            Id = userId,
            Email = email ?? $"testuser{userId:N}@example.com",
            NormalizedEmail = (email ?? $"testuser{userId:N}@example.com").ToUpperInvariant(),
            UserName = username ?? $"testuser{userId:N}",
            PasswordHash = "TestHashedPassword123",
            SecurityStamp = Guid.NewGuid().ToString(),
            EmailConfirmed = emailConfirmed,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "TestSystem"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return userId;
    }

    /// <summary>
    /// Creates and seeds a test product.
    /// </summary>
    public static async Task<Guid> SeedProductAsync(
        ApplicationDbContext context,
        string? name = null,
        decimal? price = null,
        Guid? ownerId = null)
    {
        ownerId ??= Guid.Parse("00000000-0000-0000-0000-000000000001");

        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = name ?? $"Test Product {productId:N}",
            Price = price ?? 99.99m,
            OwnerId = ownerId.Value,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "TestSystem"
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return productId;
    }

    /// <summary>
    /// Creates and seeds multiple test products.
    /// </summary>
    public static async Task<List<Guid>> SeedProductsAsync(
        ApplicationDbContext context,
        int count = 3,
        Guid? ownerId = null)
    {
        ownerId ??= Guid.Parse("00000000-0000-0000-0000-000000000001");

        var productIds = new List<Guid>();

        for (int i = 0; i < count; i++)
        {
            var productId = await SeedProductAsync(
                context,
                name: $"Test Product {i + 1}",
                price: (i + 1) * 10.99m,
                ownerId: ownerId);
            productIds.Add(productId);
        }

        return productIds;
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public static async Task ClearAllDataAsync(ApplicationDbContext context)
    {
        context.Products.RemoveRange(context.Products);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
    }
}
