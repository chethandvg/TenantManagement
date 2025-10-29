using System.Threading.Tasks;
using Archu.Ui.Layouts;
using Archu.Ui.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Archu.Ui.Tests.Layouts;

/// <summary>
/// Validates that <see cref="MainLayout"/> exposes the expected accessibility landmarks and remains axe compliant.
/// </summary>
public sealed class MainLayoutAccessibilityTests
{
    /// <summary>
    /// Ensures the layout renders key ARIA landmarks, skip links, and passes automated accessibility verification.
    /// </summary>
    [Fact]
    public async Task MainLayout_ShouldExposeLandmarks_AndPassAxeScanAsync()
    {
        using var ctx = UiTestContextFactory.Create();
        UiTestContextFactory.SetAuthenticatedUser(ctx, "test-user");

        var cut = ctx.RenderComponent<CascadingAuthenticationState>(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<MainLayout>(0);
                builder.CloseComponent();
            }));
        });

        var skipLink = cut.Find("a.archu-skip-link");
        Assert.Equal("#main-content", skipLink.GetAttribute("href"));

        var header = cut.Find("header.mud-appbar");
        Assert.Contains("mud-appbar", header.ClassList);

        var toggleButton = cut.Find("button[aria-label='Toggle navigation menu']");
        Assert.NotNull(toggleButton);

        var navigation = cut.Find("nav[aria-labelledby='drawer-title']");
        Assert.Equal("NAV", navigation.TagName);

        var drawerTitle = cut.Find("#drawer-title");
        Assert.Equal("Navigation", drawerTitle.TextContent.Trim());

        var main = cut.Find("main#main-content[role='main']");
        Assert.Equal("-1", main.GetAttribute("tabindex"));

        await cut.VerifyAccessibilityAsync();
    }
}
