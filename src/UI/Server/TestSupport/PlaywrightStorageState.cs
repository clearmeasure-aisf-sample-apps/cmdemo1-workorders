using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Playwright <c>storageState</c> JSON shape.</summary>
public sealed class PlaywrightStorageState
{
    /// <summary>Cookie list (empty for localStorage-only sign-in).</summary>
    [JsonPropertyName("cookies")]
    public required IReadOnlyList<object> Cookies { get; init; }

    /// <summary>Per-origin storage.</summary>
    [JsonPropertyName("origins")]
    public required IReadOnlyList<PlaywrightOrigin> Origins { get; init; }
}
