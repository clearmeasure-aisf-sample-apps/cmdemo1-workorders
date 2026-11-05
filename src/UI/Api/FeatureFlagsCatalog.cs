namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Code defaults for application feature flags. <see cref="IFeatureFlagRuntime"/> overlays the
/// <c>FeatureFlags</c> configuration section on this map. Add or remove a flag only here.
/// </summary>
public static class FeatureFlagsCatalog
{
    /// <summary>Buildings and rooms administration. Code default is off.</summary>
    public const string BuildingManagement = "BuildingManagement";

    /// <summary>Vendor administration. Code default is off. Teams stay available.</summary>
    public const string VendorManagement = "VendorManagement";

    /// <summary>
    /// Code default for every known feature flag. Configuration may override a known name at runtime.
    /// </summary>
    public static IReadOnlyDictionary<string, bool> All { get; } =
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["SampleFeatureA"] = true,
            ["SampleFeatureB"] = false,
            ["ShowPriorityBadges"] = true,
            [BuildingManagement] = false,
            [VendorManagement] = false
        };
}
