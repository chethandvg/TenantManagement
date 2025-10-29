using System;
using MudBlazor;

namespace Archu.Ui.Theming;

/// <summary>
/// Describes a service capable of exposing the current design tokens and MudBlazor theme
/// used by Archu.Ui. Consumers can subscribe to changes and apply overrides at runtime.
/// </summary>
public interface IThemeTokenService
{
    /// <summary>
    /// Occurs when the design tokens have been updated and a new theme snapshot is available.
    /// </summary>
    event EventHandler<DesignTokens>? TokensChanged;

    /// <summary>
    /// Gets a copy of the current design token set.
    /// </summary>
    /// <returns>A cloned <see cref="DesignTokens"/> instance that represents the active theme.</returns>
    DesignTokens GetTokens();

    /// <summary>
    /// Gets the current MudBlazor theme instance built from the active tokens.
    /// </summary>
    /// <returns>The <see cref="MudTheme"/> that should be supplied to <see cref="MudThemeProvider"/>.</returns>
    MudTheme GetMudTheme();

    /// <summary>
    /// Applies token overrides by cloning the current token set, executing the provided callback,
    /// and publishing the updated tokens to listeners.
    /// </summary>
    /// <param name="configure">The callback that can modify the cloned token set.</param>
    void ApplyOverrides(Action<DesignTokens> configure);
}
