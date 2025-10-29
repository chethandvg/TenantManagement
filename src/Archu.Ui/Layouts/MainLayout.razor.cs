using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archu.ApiClient.Authentication.Services;
using Archu.Ui.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Archu.Ui.Layouts;

/// <summary>
/// Provides the application-wide layout structure, including navigation drawer and authentication controls.
/// </summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;
    private MudTheme _theme = default!;
    private static readonly Dictionary<string, object> DrawerAccessibilityAttributes = new(StringComparer.Ordinal)
    {
        { "role", "presentation" }
    };
    private static readonly RenderFragment AccountMenuActivator = builder =>
    {
        builder.OpenComponent<MudIconButton>(0);
        builder.AddAttribute(1, nameof(MudIconButton.Icon), Icons.Material.Filled.AccountCircle);
        builder.AddAttribute(2, nameof(MudIconButton.Color), Color.Inherit);
        builder.AddAttribute(3, "aria-label", "Open account menu");
        builder.CloseComponent();
    };

    [Inject]
    private IAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IThemeTokenService ThemeService { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _theme = ThemeService.GetMudTheme();
        ThemeService.TokensChanged += HandleTokensChanged;
    }

    /// <summary>
    /// Toggles the navigation drawer to switch between expanded and collapsed states
    /// whenever the app bar menu button is activated.
    /// </summary>
    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    /// <summary>
    /// Handles keyboard interactions within the navigation drawer so pressing Escape
    /// closes it and prevents keyboard users from getting trapped inside the menu.
    /// </summary>
    /// <param name="args">The keyboard event triggered from the drawer element.</param>
    private void HandleDrawerKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && _drawerOpen)
        {
            _drawerOpen = false;
        }
    }

    /// <summary>
    /// Performs the logout workflow by requesting sign-out from the authentication service,
    /// displaying a confirmation snackbar, and redirecting the user to the login page.
    /// </summary>
    private async Task HandleLogoutAsync()
    {
        await AuthService.LogoutAsync();
        Snackbar.Add("Logged out successfully", Severity.Info);
        Navigation.NavigateTo("/login", forceLoad: true);
    }

    /// <summary>
    /// Refreshes the MudBlazor theme and triggers a render whenever the theme tokens change.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The updated token snapshot, unused because the layout fetches the latest theme from the service.</param>
    private void HandleTokensChanged(object? sender, ThemeTokensChangedEventArgs args)
    {
        _theme = ThemeService.GetMudTheme();
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Disposes resources by unsubscribing from theme change notifications.
    /// </summary>
    public void Dispose()
    {
        ThemeService.TokensChanged -= HandleTokensChanged;
    }
}
