using System;
using TentMan.Ui.State;
using TentMan.Ui.Theming;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace TentMan.Ui;

/// <summary>
/// Platform-agnostic service registration for TentMan.Ui components.
/// Works with Blazor Server, WebAssembly, and Blazor Hybrid (MAUI).
/// </summary>
public static class UiServiceCollectionExtensions
{
    /// <summary>
    /// Registers MudBlazor, theming, and TentMan.Ui services without platform-specific dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureTheme">Optional callback used to override default TentMan design tokens.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTentManUi(this IServiceCollection services, Action<ThemeOptions>? configureTheme = null)
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

        // Register theming services that surface design tokens and MudBlazor theme instances
        services.AddTentManTheming(configureTheme);

        // Register shared UI state containers that power busy and error workflows
        services.AddScoped<BusyState>();
        services.AddScoped<UiState>();

        return services;
    }
}
