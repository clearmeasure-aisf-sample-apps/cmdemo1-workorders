using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.UI.Shared.Services;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

/// <summary>
/// In-memory feature flags for component tests.
/// <see cref="FeatureFlagNames.ShowPriorityBadges"/>, <see cref="FeatureFlagNames.BuildingManagement"/>,
/// and <see cref="FeatureFlagNames.VendorManagement"/> default to enabled.
/// </summary>
public sealed class StubFeatureFlags(
    bool showPriorityBadges = true,
    bool buildingManagement = true,
    bool vendorManagement = true) : IFeatureFlags
{
    private readonly Dictionary<string, bool> _flags = new(StringComparer.Ordinal)
    {
        [FeatureFlagNames.ShowPriorityBadges] = showPriorityBadges,
        [FeatureFlagNames.BuildingManagement] = buildingManagement,
        [FeatureFlagNames.VendorManagement] = vendorManagement
    };

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string name) =>
        Task.FromResult(_flags.TryGetValue(name, out var enabled) && enabled);
}
