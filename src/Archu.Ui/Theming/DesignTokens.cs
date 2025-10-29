namespace Archu.Ui.Theming;

/// <summary>
/// Represents the full set of design tokens used by Archu.Ui components.
/// Tokens are split into color, typography, and layout groups for clarity.
/// </summary>
public sealed class DesignTokens
{
    /// <summary>
    /// Gets or sets the color token group for the theme.
    /// </summary>
    public ColorTokens Colors { get; set; } = new();

    /// <summary>
    /// Gets or sets the typography token group for the theme.
    /// </summary>
    public TypographyTokens Typography { get; set; } = new();

    /// <summary>
    /// Gets or sets the layout-related token group for the theme.
    /// </summary>
    public LayoutTokens Layout { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of the current token set so that consumers can safely modify instances.
    /// </summary>
    /// <returns>A new <see cref="DesignTokens"/> with the same values as the current instance.</returns>
    public DesignTokens Clone()
    {
        return new DesignTokens
        {
            Colors = Colors.Clone(),
            Typography = Typography.Clone(),
            Layout = Layout.Clone(),
        };
    }
}

/// <summary>
/// Represents the color design tokens that map onto MudBlazor palette entries and CSS variables.
/// </summary>
public sealed class ColorTokens
{
    /// <summary>
    /// Gets or sets the primary brand color.
    /// </summary>
    public string Primary { get; set; } = "#6750A4";

    /// <summary>
    /// Gets or sets the foreground color that should be used on top of the primary color.
    /// </summary>
    public string OnPrimary { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the secondary accent color.
    /// </summary>
    public string Secondary { get; set; } = "#625B71";

    /// <summary>
    /// Gets or sets the foreground color that should be used on top of the secondary color.
    /// </summary>
    public string OnSecondary { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the neutral background color for the application.
    /// </summary>
    public string Background { get; set; } = "#FFFBFE";

    /// <summary>
    /// Gets or sets the foreground color that should be used on the background.
    /// </summary>
    public string OnBackground { get; set; } = "#1C1B1F";

    /// <summary>
    /// Gets or sets the surface color used for cards and panels.
    /// </summary>
    public string Surface { get; set; } = "#FFFBFE";

    /// <summary>
    /// Gets or sets the color of alternative surfaces such as selected list items.
    /// </summary>
    public string SurfaceVariant { get; set; } = "#E7E0EC";

    /// <summary>
    /// Gets or sets the emphasis color for success messaging.
    /// </summary>
    public string Success { get; set; } = "#0F7A4A";

    /// <summary>
    /// Gets or sets the emphasis color for warnings.
    /// </summary>
    public string Warning { get; set; } = "#FFB74D";

    /// <summary>
    /// Gets or sets the emphasis color for informational messaging.
    /// </summary>
    public string Info { get; set; } = "#2196F3";

    /// <summary>
    /// Gets or sets the emphasis color for error states.
    /// </summary>
    public string Error { get; set; } = "#B3261E";

    /// <summary>
    /// Gets or sets the color used for outlines and dividers.
    /// </summary>
    public string Outline { get; set; } = "#79747E";

    /// <summary>
    /// Creates a deep copy of the current color token set.
    /// </summary>
    /// <returns>A new <see cref="ColorTokens"/> with the same values.</returns>
    public ColorTokens Clone()
    {
        return (ColorTokens)MemberwiseClone();
    }
}

/// <summary>
/// Represents typography tokens such as font families and weights.
/// </summary>
public sealed class TypographyTokens
{
    /// <summary>
    /// Gets or sets the base font family for the application.
    /// </summary>
    public string FontFamily { get; set; } = "'Roboto','Helvetica','Arial',sans-serif";

    /// <summary>
    /// Gets or sets the font weight for headings.
    /// </summary>
    public string HeadingFontWeight { get; set; } = "600";

    /// <summary>
    /// Gets or sets the font weight for body text.
    /// </summary>
    public string BodyFontWeight { get; set; } = "400";

    /// <summary>
    /// Gets or sets the base font size for the application.
    /// </summary>
    public string BaseFontSize { get; set; } = "16px";

    /// <summary>
    /// Creates a deep copy of the current typography token set.
    /// </summary>
    /// <returns>A new <see cref="TypographyTokens"/> with the same values.</returns>
    public TypographyTokens Clone()
    {
        return (TypographyTokens)MemberwiseClone();
    }
}

/// <summary>
/// Represents layout tokens such as border radius and spacing values.
/// </summary>
public sealed class LayoutTokens
{
    /// <summary>
    /// Gets or sets the default border radius applied to components.
    /// </summary>
    public string BorderRadius { get; set; } = "12px";

    /// <summary>
    /// Gets or sets the elevation shadow used by raised surfaces.
    /// </summary>
    public string Shadow { get; set; } = "0 8px 16px rgba(17, 17, 26, 0.1)";

    /// <summary>
    /// Gets or sets the default spacing unit used for gutters and padding.
    /// </summary>
    public string SpacingUnit { get; set; } = "8px";

    /// <summary>
    /// Creates a deep copy of the current layout token set.
    /// </summary>
    /// <returns>A new <see cref="LayoutTokens"/> with the same values.</returns>
    public LayoutTokens Clone()
    {
        return (LayoutTokens)MemberwiseClone();
    }
}
