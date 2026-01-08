using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace TentMan.ApiClient.Tests.TestHelpers.Fixtures;

/// <summary>
/// Combines AutoFixture with AutoMoq for automatic test data generation and mocking.
/// This attribute automatically injects mocked dependencies and generates test data.
/// </summary>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() => new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new ApiResponseCustomization()))
    {
    }
}

/// <summary>
/// Customization for API response DTOs to ensure proper initialization.
/// </summary>
public class ApiResponseCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Contracts.Products.ProductDto>(composer =>
            composer
                .With(p => p.RowVersion, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                .With(p => p.Name, () => fixture.Create<string>())
                .With(p => p.Price, () => Math.Round(fixture.Create<decimal>() % 10000, 2)));
    }
}
