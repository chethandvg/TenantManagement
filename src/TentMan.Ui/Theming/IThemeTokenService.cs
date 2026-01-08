using System;
using MudBlazor;

namespace TentMan.Ui.Theming;

/// <summary>
/// Describes a service capable of exposing the current design tokens and MudBlazor theme
/// used by TentMan.Ui. Consumers can subscribe to changes and apply overrides at runtime.
/// </summary>
public interface IThemeTokenService
{
    /// <summary>
    /// Occurs when the design tokens have been updated and a new theme snapshot is available.
    /// </summary>
    event EventHandler<ThemeTokensChangedEventArgs>? TokensChanged;

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

/// <summary>
/// Provides event data for <see cref="IThemeTokenService"/> token change notifications.
/// </summary>
public sealed class ThemeTokensChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeTokensChangedEventArgs"/> class.
    /// </summary>
    /// <param name="tokens">The updated design tokens associated with the event.</param>
    public ThemeTokensChangedEventArgs(DesignTokens tokens)
    {
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    /// <summary>
    /// Gets the updated design tokens representing the current theme snapshot.
    /// </summary>
    public DesignTokens Tokens { get; }
}
