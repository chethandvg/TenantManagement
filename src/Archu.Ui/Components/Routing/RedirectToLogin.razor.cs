using Microsoft.AspNetCore.Components;

namespace Archu.Ui.Components.Routing;

/// <summary>
/// This component handles redirecting unauthenticated users to the login page while preserving the return URL.
/// </summary>
public partial class RedirectToLogin : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Triggers navigation to the login page with the current URI as the return target
    /// as soon as the component initializes, ensuring protected routes redirect properly.
    /// </summary>
    protected override void OnInitialized()
    {
        Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}");
    }
}
