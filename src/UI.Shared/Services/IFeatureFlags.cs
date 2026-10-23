namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Reads runtime feature flags for the user interface.
/// </summary>
public interface IFeatureFlags
{
    /// <summary>
    /// Whether the named flag is enabled.
    /// </summary>
    Task<bool> IsEnabledAsync(string name);
}

/// <summary>
/// Names of feature flags the user interface reads.
/// </summary>
public static class FeatureFlagNames
{
    /// <summary>
    /// When true, High priority rows on the work order list show a colored badge.
    /// </summary>
    public const string ShowPriorityBadges = "ShowPriorityBadges";
}
