using AutoFixture.Xunit2;
using Xunit;

namespace TentMan.UnitTests.TestHelpers.Fixtures;

/// <summary>
/// Combines InlineData with AutoMoqData for parameterized tests with automatic test data generation.
/// Allows mixing explicit parameter values with auto-generated ones.
/// </summary>
/// <example>
/// Usage:
/// <code>
/// [Theory, InlineAutoMoqData("Special Product", 123.45)]
/// public async Task Test_Method(
///     string productName,  // from InlineData
///     decimal price,       // from InlineData
///     Guid userId,         // auto-generated
///     [Frozen] Mock&lt;IProductRepository&gt; mockRepository)  // auto-mocked
/// {
///     // Test implementation
/// }
/// </code>
/// </example>
public class InlineAutoMoqDataAttribute : InlineAutoDataAttribute
{
    public InlineAutoMoqDataAttribute(params object[] values)
        : base(new AutoMoqDataAttribute(), values)
    {
    }
}
