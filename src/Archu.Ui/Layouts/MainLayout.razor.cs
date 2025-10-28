using Archu.ApiClient.Authentication.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Archu.Ui.Layouts;

public partial class MainLayout
{
    private bool _drawerOpen = true;

    [Inject]
    private IAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

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
}
