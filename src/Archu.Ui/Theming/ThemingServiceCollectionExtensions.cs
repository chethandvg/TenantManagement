using System;
using Microsoft.Extensions.DependencyInjection;

namespace Archu.Ui.Theming;

/// <summary>
/// Provides service registration helpers for Archu.Ui theming components.
/// </summary>
public static class ThemingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default theming services and allows callers to customize the initial theme options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback used to override theme options and tokens.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddArchuTheming(this IServiceCollection services, Action<ThemeOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var options = new ThemeOptions();
        configure?.Invoke(options);

        var configuredOptions = options.Clone();

        services.AddSingleton(configuredOptions);
        services.AddSingleton<IThemeTokenService>(sp => new ThemeTokenService(configuredOptions));

        return services;
    }
}
