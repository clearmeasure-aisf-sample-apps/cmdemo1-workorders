using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Runtime feature-flag map shared by the flags endpoint and diagnostics.
/// Code defaults come from <see cref="FeatureFlagsCatalog.All"/>.
/// A value in the <c>FeatureFlags</c> configuration section overrides a known name.
/// Unknown names are ignored. A value that is not a boolean leaves the code default.
/// </summary>
public interface IFeatureFlagRuntime
{
    /// <summary>Current enabled state of every known flag.</summary>
    IReadOnlyDictionary<string, bool> Snapshot();
}

/// <summary>
/// Builds <see cref="IFeatureFlagRuntime.Snapshot"/> from code defaults plus configuration.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global -- registered in ServerApplication
public sealed class ConfigurationFeatureFlagRuntime(IConfiguration configuration) : IFeatureFlagRuntime
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, bool> Snapshot()
    {
        var snapshot = new Dictionary<string, bool>(FeatureFlagsCatalog.All, StringComparer.Ordinal);
        foreach (var child in configuration.GetSection(DiagnosticsFeatureFlagsOptions.SectionName).GetChildren())
        {
            if (!snapshot.ContainsKey(child.Key))
            {
                continue;
            }

            if (bool.TryParse(child.Value, out var enabled))
            {
                snapshot[child.Key] = enabled;
            }
        }

        return snapshot;
    }
}
