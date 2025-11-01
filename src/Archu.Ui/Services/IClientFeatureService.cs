namespace Archu.Ui.Services;

/// <summary>
/// Provides a strongly-typed abstraction for querying client-side feature flag state.
/// </summary>
public interface IClientFeatureService
{
    /// <summary>
    /// Determines whether the supplied feature flag is currently enabled.
    /// </summary>
    /// <param name="featureName">The feature flag identifier to evaluate.</param>
    /// <returns>A task that resolves to <c>true</c> when the feature is enabled.</returns>
    Task<bool> IsEnabledAsync(string featureName);
}
