using System.Threading.Tasks;
using Archu.Ui.Components.Navigation;
using Archu.Ui.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace Archu.Ui.Tests.Components.Navigation;

/// <summary>
/// Validates accessibility behaviors for the <see cref="NavMenu"/> component.
/// </summary>
public sealed class NavMenuAccessibilityTests
{
    /// <summary>
    /// Ensures the navigation menu exposes an accessible landmark and label while passing axe validation.
    /// </summary>
    [Fact]
    public async Task NavMenu_ShouldExposeNavigationLandmark_AndPassAxeScanAsync()
    {
        using var ctx = UiTestContextFactory.Create();
        UiTestContextFactory.SetAuthenticatedUser(ctx, "test-user");

        var cut = ctx.RenderComponent<CascadingAuthenticationState>(parameters =>
        {
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<NavMenu>(0);
                builder.CloseComponent();
            }));
        });

        var navigation = cut.Find("nav[role='navigation']");
        Assert.Equal("Primary navigation", navigation.GetAttribute("aria-label"));

        await cut.VerifyAccessibilityAsync();
    }
}
