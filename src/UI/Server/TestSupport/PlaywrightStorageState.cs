using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>Playwright <c>storageState</c> JSON shape.</summary>
public sealed record PlaywrightStorageState(
    [property: JsonPropertyName("cookies")] IReadOnlyList<object> Cookies,
    [property: JsonPropertyName("origins")] IReadOnlyList<PlaywrightOrigin> Origins);
