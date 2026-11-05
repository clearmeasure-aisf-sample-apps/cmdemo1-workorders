namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>Reads whether a named feature flag is enabled, for hiding UI. Server-side gates use the bus.</summary>
public interface IFeatureFlags
{
    /// <summary>Returns whether <paramref name="name"/> is enabled.</summary>
    Task<bool> IsEnabledAsync(string name);
}
