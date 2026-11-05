namespace ClearMeasure.Bootcamp.Core.FeatureFlags;

/// <summary>
/// Thrown when a bus request requires a feature flag that is disabled. Hosts map it to a 403 problem
/// response (single API) or tool error text (MCP).
/// </summary>
public sealed class FeatureDisabledException(string featureName, string requestName)
    : Exception($"The {featureName} feature is disabled, so {requestName} cannot run.")
{
    /// <summary>The disabled feature flag name.</summary>
    public string FeatureName { get; } = featureName;

    /// <summary>The request type name that was rejected.</summary>
    public string RequestName { get; } = requestName;
}
