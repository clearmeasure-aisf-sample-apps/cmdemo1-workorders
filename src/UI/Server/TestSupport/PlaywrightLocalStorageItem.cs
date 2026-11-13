using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>One localStorage entry inside Playwright storage state.</summary>
public sealed class PlaywrightLocalStorageItem
{
    /// <summary>localStorage key.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>localStorage value.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
