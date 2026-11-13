using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>One origin block inside Playwright storage state.</summary>
// ReSharper disable NotAccessedPositionalProperty.Global -- serialized to Playwright storageState JSON
public sealed record PlaywrightOrigin(
    [property: JsonPropertyName("origin")] string Origin,
    [property: JsonPropertyName("localStorage")] IReadOnlyList<PlaywrightLocalStorageItem> LocalStorage);
