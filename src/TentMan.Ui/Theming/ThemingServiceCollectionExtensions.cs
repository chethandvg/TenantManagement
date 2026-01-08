using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TentMan.Ui.Theming;

/// <summary>
/// Provides service registration helpers for TentMan.Ui theming components.
/// </summary>
public static class ThemingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default theming services and allows callers to customize the initial theme options.
    /// The service lifetime is scoped so that Blazor Server circuits receive isolated theme state while
    /// WebAssembly apps continue to observe singleton-like behavior per client instance.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback used to override theme options and tokens.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTentManTheming(this IServiceCollection services, Action<ThemeOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var optionsBuilder = services.AddOptions<ThemeOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.AddScoped<IThemeTokenService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ThemeOptions>>().Value.Clone();
            return new ThemeTokenService(options);
        });

        return services;
    }
}
