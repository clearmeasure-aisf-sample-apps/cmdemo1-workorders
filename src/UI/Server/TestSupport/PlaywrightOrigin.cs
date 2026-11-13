using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>One origin block inside Playwright storage state.</summary>
public sealed class PlaywrightOrigin
{
    /// <summary>Origin URL.</summary>
    [JsonPropertyName("origin")]
    public required string Origin { get; init; }

    /// <summary>localStorage entries for the origin.</summary>
    [JsonPropertyName("localStorage")]
    public required IReadOnlyList<PlaywrightLocalStorageItem> LocalStorage { get; init; }
}
