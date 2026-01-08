using System;

namespace TentMan.Ui.Theming;

/// <summary>
/// Defines configuration options for TentMan.Ui theming, including default design tokens
/// and the CSS variable prefix used when generating token outputs.
/// </summary>
public sealed class ThemeOptions
{
    /// <summary>
    /// Gets or sets the prefix applied to generated CSS variables, such as <c>--tentman-color-primary</c>.
    /// </summary>
    public string CssVariablePrefix { get; set; } = "tentman";

    /// <summary>
    /// Gets or sets the initial design token set applied to the theme.
    /// </summary>
    public DesignTokens Tokens { get; set; } = new();

    /// <summary>
    /// Gets or sets an optional callback that can apply additional customization to the MudBlazor theme instance.
    /// </summary>
    public Action<MudBlazor.MudTheme>? ConfigureMudTheme { get; set; }
        = null;

    /// <summary>
    /// Creates a deep copy of the current options to protect against accidental shared state mutations.
    /// </summary>
    /// <returns>A new <see cref="ThemeOptions"/> instance with the same values.</returns>
    public ThemeOptions Clone()
    {
        return new ThemeOptions
        {
            CssVariablePrefix = CssVariablePrefix,
            Tokens = Tokens.Clone(),
            ConfigureMudTheme = ConfigureMudTheme,
        };
    }
}
