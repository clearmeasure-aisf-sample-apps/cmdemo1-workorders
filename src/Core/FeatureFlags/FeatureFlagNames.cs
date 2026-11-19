namespace ClearMeasure.Bootcamp.Core.FeatureFlags;

/// <summary>
/// The fixed catalog of feature flag names. Every name is evaluated at runtime; a name that no provider
/// knows evaluates to false.
/// </summary>
public static class FeatureFlagNames
{
    /// <summary>When enabled, High-priority work orders show a colored badge on the search list and board.</summary>
    public const string ShowPriorityBadges = "ShowPriorityBadges";

    /// <summary>When enabled, the Buildings &amp; Rooms admin surface and its building-room summary reads are available.</summary>
    public const string BuildingManagement = "BuildingManagement";

    /// <summary>When enabled, the vendor directory (list and create, UI and MCP) is available.</summary>
    public const string VendorManagement = "VendorManagement";

    /// <summary>When enabled, the work order search page shows an early-access banner.</summary>
    public const string EarlyAccessBanner = "EarlyAccessBanner";

    /// <summary>Every catalog name, in the order the flags endpoint reports them.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ShowPriorityBadges,
        BuildingManagement,
        VendorManagement,
        EarlyAccessBanner
    ];
}
