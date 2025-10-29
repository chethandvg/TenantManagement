using System;
using System.Threading.Tasks;
using Archu.ApiClient.Authentication.Services;
using Archu.Ui.Theming;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Archu.Ui.Layouts;

/// <summary>
/// Provides the application-wide layout structure, including navigation drawer and authentication controls.
/// </summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;
    private MudTheme _theme = default!;

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
