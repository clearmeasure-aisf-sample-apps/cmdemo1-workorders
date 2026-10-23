using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared.Services;

namespace ClearMeasure.Bootcamp.UI.Client;

/// <summary>
/// Reads feature flags from <c>GET api/features/flags</c>.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global -- registered in UIClientServiceRegistry
public sealed class HttpFeatureFlags(HttpClient httpClient) : IFeatureFlags
{
    private const string FlagsPath = "api/features/flags";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private Dictionary<string, bool>? _flags;
    private bool _loaded;

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string name)
    {
        var flags = await LoadAsync();
        if (flags is null)
        {
            return name == FeatureFlagNames.ShowPriorityBadges;
        }

        return flags.TryGetValue(name, out var enabled) && enabled;
    }

    private async Task<Dictionary<string, bool>?> LoadAsync()
    {
        if (_loaded)
        {
            return _flags;
        }

        try
        {
            // ReSharper disable once MethodSupportsCancellation -- IFeatureFlags has no CancellationToken
            using var response = await httpClient.GetAsync(FlagsPath);
            if (!response.IsSuccessStatusCode)
            {
                _loaded = true;
                return null;
            }

            // ReSharper disable once MethodSupportsCancellation -- IFeatureFlags has no CancellationToken
            _flags = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>(JsonOptions);
            _loaded = true;
            return _flags;
        }
        catch (HttpRequestException)
        {
            _loaded = true;
            return null;
        }
        catch (JsonException)
        {
            _loaded = true;
            return null;
        }
        catch (NotSupportedException)
        {
            _loaded = true;
            return null;
        }
    }
}
