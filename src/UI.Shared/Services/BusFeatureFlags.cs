using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Sends <see cref="FeatureFlagsQuery"/> once per scope and answers flag lookups from the result.
/// A failed query uses the catalog default for <see cref="FeatureFlagNames.ShowPriorityBadges"/> and false for
/// every other name. A result that omits a key answers false for that key.
/// </summary>
public sealed class BusFeatureFlags(IBus bus) : IFeatureFlags
{
    private Task<IReadOnlyDictionary<string, bool>?>? _flags;

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string name)
    {
        var flags = await (_flags ??= LoadAsync());
        if (flags is null)
        {
            return name == FeatureFlagNames.ShowPriorityBadges;
        }

        return flags.TryGetValue(name, out bool enabled) && enabled;
    }

    private async Task<IReadOnlyDictionary<string, bool>?> LoadAsync()
    {
        try
        {
            var result = await bus.Send(new FeatureFlagsQuery());
            return result.Flags;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or InvalidOperationException or JsonException)
        {
            return null;
        }
    }
}
