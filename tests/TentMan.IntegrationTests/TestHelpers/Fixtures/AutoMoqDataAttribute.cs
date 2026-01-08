using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace TentMan.IntegrationTests.TestHelpers.Fixtures;

/// <summary>
/// Combines AutoFixture with AutoMoq for automatic test data generation and mocking.
/// This attribute automatically injects mocked dependencies and generates test data.
/// </summary>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() => new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new IntegrationTestCustomization()))
    {
    }
}

/// <summary>
/// Customization for integration test entities and DTOs.
/// </summary>
public class IntegrationTestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Domain.Entities.Product>(composer =>
            composer
                .With(p => p.RowVersion, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                .With(p => p.Name, () => fixture.Create<string>())
                .With(p => p.Price, () => Math.Round(fixture.Create<decimal>() % 10000, 2))
                .With(p => p.IsDeleted, false)
                .With(p => p.CreatedAtUtc, DateTime.UtcNow)
                .With(p => p.ModifiedAtUtc, (DateTime?)null));

        fixture.Customize<Contracts.Products.CreateProductRequest>(composer =>
            composer
                .With(p => p.Name, () => fixture.Create<string>())
                .With(p => p.Price, () => Math.Round(fixture.Create<decimal>() % 10000, 2)));

        fixture.Customize<Contracts.Products.UpdateProductRequest>(composer =>
            composer
                .With(p => p.RowVersion, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                .With(p => p.Name, () => fixture.Create<string>())
                .With(p => p.Price, () => Math.Round(fixture.Create<decimal>() % 10000, 2)));
    }
}
