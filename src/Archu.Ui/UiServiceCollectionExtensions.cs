using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Archu.Ui;

/// <summary>
/// Platform-agnostic service registration for Archu.Ui components.
/// Works with Blazor Server, WebAssembly, and Blazor Hybrid (MAUI).
/// </summary>
public static class UiServiceCollectionExtensions
{
    /// <summary>
    /// Registers MudBlazor and Archu.Ui services without platform-specific dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddArchuUi(this IServiceCollection services)
    {
        // Register MudBlazor services
        services.AddMudServices(config =>
        {
            // Default configuration - consumers can override if needed
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
        });

        // Add any custom UI services here in the future
        return services;
    }
}
