namespace Archu.Infrastructure.Contentful;

/// <summary>
/// Configuration options for Contentful CMS integration.
/// These settings should be configured in appsettings.json.
/// </summary>
public sealed class ContentfulSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Contentful";

    /// <summary>
    /// The Contentful Space ID.
    /// This identifies your Contentful space.
    /// </summary>
    public string SpaceId { get; init; } = string.Empty;

    /// <summary>
    /// The Contentful Content Delivery API key.
    /// Used to access published content.
    /// Should be stored securely (Azure Key Vault, environment variables, etc.).
    /// </summary>
    public string DeliveryApiKey { get; init; } = string.Empty;

    /// <summary>
    /// The Contentful environment name.
    /// Default: "master"
    /// </summary>
    public string Environment { get; init; } = "master";

    /// <summary>
    /// Validates that the Contentful options are properly configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SpaceId))
            throw new InvalidOperationException("Contentful SpaceId is not configured. Please set Contentful:SpaceId in appsettings.json or environment variables.");

        if (string.IsNullOrWhiteSpace(DeliveryApiKey))
            throw new InvalidOperationException("Contentful DeliveryApiKey is not configured. Please set Contentful:DeliveryApiKey in appsettings.json or environment variables.");

        if (string.IsNullOrWhiteSpace(Environment))
            throw new InvalidOperationException("Contentful Environment is not configured. Please set Contentful:Environment in appsettings.json.");
    }
}
