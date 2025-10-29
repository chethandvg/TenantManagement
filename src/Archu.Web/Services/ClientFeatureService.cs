using Microsoft.FeatureManagement;

namespace Archu.Web.Services;

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

/// <summary>
/// Default <see cref="IClientFeatureService"/> implementation that delegates to <see cref="IFeatureManager"/>.
/// </summary>
public sealed class ClientFeatureService : IClientFeatureService
{
    private readonly IFeatureManager _featureManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientFeatureService"/> class.
    /// </summary>
    /// <param name="featureManager">Feature manager used to evaluate flag state.</param>
    public ClientFeatureService(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    /// <summary>
    /// Determines whether the supplied feature flag is currently enabled.
    /// </summary>
    /// <param name="featureName">The feature flag identifier to evaluate.</param>
    /// <returns>A task that resolves to <c>true</c> when the feature is enabled.</returns>
    public Task<bool> IsEnabledAsync(string featureName)
    {
        return _featureManager.IsEnabledAsync(featureName);
    }
}
