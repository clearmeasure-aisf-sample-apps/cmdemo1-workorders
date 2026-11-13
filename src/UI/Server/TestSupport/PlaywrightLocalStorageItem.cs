using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>One localStorage entry inside Playwright storage state.</summary>
// ReSharper disable NotAccessedPositionalProperty.Global -- serialized to Playwright storageState JSON
public sealed record PlaywrightLocalStorageItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("value")] string Value);
