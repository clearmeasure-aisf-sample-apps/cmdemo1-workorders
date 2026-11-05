namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Names the <c>FeatureFlags</c> configuration section read by <see cref="IFeatureFlagRuntime"/>.
/// Diagnostics and the flags endpoint both use that runtime, not a separate options object.
/// </summary>
public static class DiagnosticsFeatureFlagsOptions
{
    /// <summary>Configuration section name (root <c>FeatureFlags</c> in appsettings).</summary>
    public const string SectionName = "FeatureFlags";
}
