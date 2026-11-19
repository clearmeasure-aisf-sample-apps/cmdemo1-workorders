using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Sends <see cref="FeatureFlagsQuery"/> once per scope and answers flag lookups from the result. The signed-in
/// username from <see cref="IUserSession"/> travels with the query as the OpenFeature targeting key.
/// A failed query uses the catalog default for <see cref="FeatureFlagNames.ShowPriorityBadges"/> and false for
/// every other name. A result that omits a key answers false for that key.
/// </summary>
public sealed class BusFeatureFlags(
    IBus bus,
    ILogger<BusFeatureFlags>? logger = null,
    IUserSession? userSession = null) : IFeatureFlags
{
    private readonly ILogger<BusFeatureFlags> _logger = logger ?? NullLogger<BusFeatureFlags>.Instance;
    private Task<IReadOnlyDictionary<string, bool>?>? _flags;

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string name)
    {
        var flags = await (_flags ??= LoadAsync());
        if (flags is null)
        {
            return name == FeatureFlagNames.ShowPriorityBadges;
        }

        return flags.TryGetValue(name, out var enabled) && enabled;
    }

    private async Task<IReadOnlyDictionary<string, bool>?> LoadAsync()
    {
        try
        {
            var userName = userSession is null ? null : await userSession.GetCurrentUserNameAsync();
            var result = await bus.Send(new FeatureFlagsQuery(userName));
            return result.Flags;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or InvalidOperationException or JsonException)
        {
            _logger.LogWarning(ex, "Feature flags query failed; using catalog defaults.");
            return null;
        }
    }
}
