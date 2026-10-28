using ClearMeasure.Bootcamp.UI.Shared.Services;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

/// <summary>In-memory feature flags for component tests. <see cref="FeatureFlagNames.ShowPriorityBadges"/> defaults to enabled.</summary>
public sealed class StubFeatureFlags(bool showPriorityBadges = true) : IFeatureFlags
{
    private readonly Dictionary<string, bool> _flags = new(StringComparer.Ordinal)
    {
        [FeatureFlagNames.ShowPriorityBadges] = showPriorityBadges
    };

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string name) =>
        Task.FromResult(_flags.TryGetValue(name, out var enabled) && enabled);
}
