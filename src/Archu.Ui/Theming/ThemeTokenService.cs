using System;
using System.Linq;
using MudBlazor;

namespace Archu.Ui.Theming;

/// <summary>
/// Default implementation of <see cref="IThemeTokenService"/> that stores token state in-memory
/// and rebuilds the MudBlazor theme whenever overrides are applied.
/// </summary>
internal sealed class ThemeTokenService : IThemeTokenService
{
    private readonly object _syncRoot = new();
    private readonly ThemeOptions _options;
    private DesignTokens _currentTokens;
    private MudTheme _currentTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeTokenService"/> class with the provided options.
    /// </summary>
    /// <param name="options">The theme options describing initial tokens and configuration.</param>
    public ThemeTokenService(ThemeOptions options)
    {
        _options = options.Clone();
        _currentTokens = _options.Tokens.Clone();
        _currentTheme = BuildMudTheme(_currentTokens, _options);
    }

    /// <inheritdoc />
    public event EventHandler<ThemeTokensChangedEventArgs>? TokensChanged;

    /// <inheritdoc />
    public DesignTokens GetTokens()
    {
        lock (_syncRoot)
        {
            return _currentTokens.Clone();
        }
    }

    /// <inheritdoc />
    public MudTheme GetMudTheme()
    {
        lock (_syncRoot)
        {
            return _currentTheme;
        }
    }

    /// <inheritdoc />
    public void ApplyOverrides(Action<DesignTokens> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        DesignTokens snapshot;
        lock (_syncRoot)
        {
            var updatedTokens = _currentTokens.Clone();
            configure(updatedTokens);
            _currentTokens = updatedTokens;
            _currentTheme = BuildMudTheme(_currentTokens, _options);
            snapshot = updatedTokens.Clone();
        }

        TokensChanged?.Invoke(this, new ThemeTokensChangedEventArgs(snapshot));
    }

    /// <summary>
    /// Rebuilds the MudBlazor theme based on the provided design tokens and options.
    /// </summary>
    /// <param name="tokens">The design tokens to convert into a theme.</param>
    /// <param name="options">The theme options that may customize the resulting theme.</param>
    /// <returns>A fully configured <see cref="MudTheme"/> instance.</returns>
    private static MudTheme BuildMudTheme(DesignTokens tokens, ThemeOptions options)
    {
        var theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = tokens.Colors.Primary,
                Secondary = tokens.Colors.Secondary,
                Background = tokens.Colors.Background,
                Surface = tokens.Colors.Surface,
                Success = tokens.Colors.Success,
                Warning = tokens.Colors.Warning,
                Info = tokens.Colors.Info,
                Error = tokens.Colors.Error,
                TextPrimary = tokens.Colors.OnBackground,
                TextSecondary = tokens.Colors.OnBackground,
                DrawerBackground = tokens.Colors.Surface,
                DrawerText = tokens.Colors.OnBackground,
                AppbarBackground = tokens.Colors.Primary,
                AppbarText = tokens.Colors.OnPrimary,
                LinesDefault = tokens.Colors.Outline,
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ParseFontFamilies(tokens.Typography.FontFamily),
                    FontWeight = tokens.Typography.BodyFontWeight,
                    FontSize = tokens.Typography.BaseFontSize,
                },
                H1 = new H1Typography
                {
                    FontFamily = ParseFontFamilies(tokens.Typography.FontFamily),
                    FontWeight = tokens.Typography.HeadingFontWeight,
                },
                H2 = new H2Typography
                {
                    FontFamily = ParseFontFamilies(tokens.Typography.FontFamily),
                    FontWeight = tokens.Typography.HeadingFontWeight,
                },
                H3 = new H3Typography
                {
                    FontFamily = ParseFontFamilies(tokens.Typography.FontFamily),
                    FontWeight = tokens.Typography.HeadingFontWeight,
                },
                Button = new ButtonTypography
                {
                    FontFamily = ParseFontFamilies(tokens.Typography.FontFamily),
                    FontWeight = tokens.Typography.HeadingFontWeight,
                },
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = tokens.Layout.BorderRadius,
            },
        };

        options.ConfigureMudTheme?.Invoke(theme);
        return theme;
    }

    /// <summary>
    /// Converts the CSS-style font family token into an array compatible with MudBlazor typography settings.
    /// </summary>
    /// <param name="fontFamily">The raw font family token value, typically a comma-separated list.</param>
    /// <returns>An array of font family names ordered by preference.</returns>
    private static string[] ParseFontFamilies(string fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return Array.Empty<string>();
        }

        // Split on commas, trim whitespace and surrounding quotes so CSS-style values map to MudBlazor expectations.
        return fontFamily
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim().Trim('\'', '"'))
            .Where(value => value.Length > 0)
            .ToArray();
    }
}
