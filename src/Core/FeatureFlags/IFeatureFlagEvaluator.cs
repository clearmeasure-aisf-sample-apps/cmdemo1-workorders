namespace ClearMeasure.Bootcamp.Core.FeatureFlags;

/// <summary>
/// Server-side runtime source of feature flag values. Only the bus feature flag behavior and the
/// <see cref="Queries.FeatureFlagsQuery"/> handler read it.
/// </summary>
public interface IFeatureFlagEvaluator
{
    /// <summary>Returns whether <paramref name="featureName"/> is enabled; unknown names are false.</summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}
