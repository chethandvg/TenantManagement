using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace TentMan.AdminApiClient.Tests.TestHelpers.Fixtures;

/// <summary>
/// AutoFixture attribute that configures AutoMoq for automatic mocking in tests.
/// </summary>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() => new Fixture().Customize(new AutoMoqCustomization()))
    {
    }
}
