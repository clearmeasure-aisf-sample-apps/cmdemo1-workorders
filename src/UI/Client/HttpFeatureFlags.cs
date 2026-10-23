using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared.Services;

namespace ClearMeasure.Bootcamp.UI.Client;

/// <summary>
/// Loads <c>api/features/flags</c> once per scope and answers flag lookups from that dictionary.
/// A failed request or a body that is not a JSON object uses the catalog default for
/// <see cref="FeatureFlagNames.ShowPriorityBadges"/> and false for every other name.
/// A successful body that omits a key answers false for that key.
/// </summary>
public sealed class HttpFeatureFlags(HttpClient httpClient) : IFeatureFlags
{
    private Task<Dictionary<string, bool>?>? _flags;

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

    private async Task<Dictionary<string, bool>?> LoadAsync()
    {
        try
        {
            using var response = await httpClient.GetAsync("api/features/flags");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return Parse(json);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private static Dictionary<string, bool>? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.True:
                        flags[property.Name] = true;
                        break;
                    case JsonValueKind.False:
                        flags[property.Name] = false;
                        break;
                }
            }

            return flags;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
