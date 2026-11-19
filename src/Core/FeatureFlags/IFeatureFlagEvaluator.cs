namespace ClearMeasure.Bootcamp.Core.FeatureFlags;

/// <summary>
/// Server-side runtime source of feature flag values. Only the bus feature flag behavior and the
/// <see cref="Queries.FeatureFlagsQuery"/> handler read it.
/// </summary>
public interface IFeatureFlagEvaluator
{
    /// <summary>Returns whether <paramref name="featureName"/> is enabled; unknown names are false.</summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether <paramref name="featureName"/> is enabled for the user <paramref name="userName"/>
    /// (the OpenFeature targeting key; null or blank evaluates without targeting). Unknown names are false.
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName, string? userName, CancellationToken cancellationToken = default);
}
