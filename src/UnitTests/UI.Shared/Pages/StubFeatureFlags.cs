using ClearMeasure.Bootcamp.UI.Shared.Services;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

/// <summary>
/// Feature-flag double for bUnit. <see cref="FeatureFlagNames.ShowPriorityBadges"/> follows the constructor flag.
/// </summary>
public sealed class StubFeatureFlags(bool showPriorityBadges = true) : IFeatureFlags
{
    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string name) =>
        Task.FromResult(name == FeatureFlagNames.ShowPriorityBadges && showPriorityBadges);
}
