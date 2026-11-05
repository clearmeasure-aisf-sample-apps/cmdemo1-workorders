namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>Names of feature flags the UI reads from the catalog.</summary>
public static class FeatureFlagNames
{
    /// <summary>When enabled, High-priority work orders show a colored badge on the search list and board.</summary>
    public const string ShowPriorityBadges = "ShowPriorityBadges";

    /// <summary>When enabled, Buildings and Rooms administration is available. Code default is off.</summary>
    public const string BuildingManagement = "BuildingManagement";

    /// <summary>When enabled, vendor administration is available. Teams stay available. Code default is off.</summary>
    public const string VendorManagement = "VendorManagement";
}

/// <summary>Reads whether a named feature flag is enabled.</summary>
public interface IFeatureFlags
{
    /// <summary>Returns whether <paramref name="name"/> is enabled.</summary>
    Task<bool> IsEnabledAsync(string name);
}
