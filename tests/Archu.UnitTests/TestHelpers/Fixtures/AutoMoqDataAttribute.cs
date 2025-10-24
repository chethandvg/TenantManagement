using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Archu.Domain.Entities;
using Archu.Domain.Entities.Identity;

namespace Archu.UnitTests.TestHelpers.Fixtures;

/// <summary>
/// Combines AutoFixture with AutoMoq for automatic test data generation and mocking.
/// This attribute automatically injects mocked dependencies and generates test data.
/// </summary>
/// <example>
/// Usage:
/// <code>
/// [Theory, AutoMoqData]
/// public async Task Test_Method(
///     [Frozen] Mock&lt;IProductRepository&gt; mockRepository,
///     GetProductsQueryHandler handler,
///     List&lt;Product&gt; products)
/// {
///     // mockRepository and handler are automatically created
///     // products is automatically populated with test data
/// }
/// </code>
/// </example>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() => new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new ProductCustomization())
            .Customize(new UserCustomization()))
    {
    }
}

/// <summary>
/// Customization for Product entity to ensure proper initialization.
/// </summary>
public class ProductCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Product>(composer =>
            composer
                .With(p => p.RowVersion, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                .With(p => p.Name, () => fixture.Create<string>())
                .With(p => p.Price, () => Math.Round(fixture.Create<decimal>() % 10000, 2))
                .With(p => p.IsDeleted, false)
                .With(p => p.CreatedAtUtc, () => DateTime.UtcNow)
                .With(p => p.ModifiedAtUtc, (DateTime?)null));
    }
}

/// <summary>
/// Customization for ApplicationUser entity to ensure proper initialization.
/// </summary>
public class UserCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<ApplicationUser>(composer =>
            composer
                .With(u => u.RowVersion, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                .With(u => u.Email, () => $"{fixture.Create<string>()}@test.com")
                .With(u => u.NormalizedEmail, (ApplicationUser u) => u.Email.ToUpperInvariant())
                .With(u => u.UserName, () => fixture.Create<string>())
                .With(u => u.SecurityStamp, () => Guid.NewGuid().ToString())
                .With(u => u.EmailConfirmed, true)
                .With(u => u.IsDeleted, false)
                .With(u => u.CreatedAtUtc, () => DateTime.UtcNow)
                .With(u => u.ModifiedAtUtc, (DateTime?)null));
    }
}
