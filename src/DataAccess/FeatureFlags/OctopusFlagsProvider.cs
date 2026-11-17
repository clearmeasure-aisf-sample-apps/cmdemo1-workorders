using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>
/// Interim OpenFeature provider for Octopus Feature Toggles. It calls the endpoint Octopus.OpenFeature 4.0.0 calls, with
/// the same bearer client identifier, server (<c>OctoToggle__Url</c> or https://features.octopus.com) and one-minute
/// refresh, and resolves toggles the same way, but reads the response through the source-generated
/// <see cref="OctopusFlagsJsonContext"/>. Octopus.OpenFeature 4.0.0 reads it with reflection-based System.Text.Json,
/// which Native AOT disables, so under AOT every flag fell back to its default.
/// <para>Temporary: when an Octopus.OpenFeature release reads its responses with a JsonSerializerContext, add the
/// package back, construct OctopusFeatureProvider in <see cref="RuntimeFeatureFlagEvaluator"/> again, and delete this
/// provider and the OctopusFlag* types.</para>
/// </summary>
internal sealed class OctopusFlagsProvider : FeatureProvider, IDisposable
{
    /// <summary>Configuration key of the Octopus Feature Toggles server (environment variable <c>OctoToggle__Url</c>).</summary>
    public const string ServerUrlKey = "OctoToggle:Url";

    private const string DefaultServerUrl = "https://features.octopus.com";
    private const string EvaluationsPath = "api/feature-flags/evaluations/v4/";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _stopping = new();
    private OctopusFlagEvaluation[] _evaluations = [];
    private Task _refreshing = Task.CompletedTask;

    /// <summary>Reads toggles for <paramref name="clientIdentifier"/>; <paramref name="handler"/> replaces the network in tests.</summary>
    public OctopusFlagsProvider(
        string clientIdentifier,
        IConfiguration configuration,
        ILogger logger,
        HttpMessageHandler? handler = null)
    {
        _logger = logger;
        _http = new HttpClient(handler ?? new HttpClientHandler(), disposeHandler: true)
        {
            BaseAddress = new Uri(configuration[ServerUrlKey] ?? DefaultServerUrl),
            Timeout = RequestTimeout
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", clientIdentifier);
        _http.DefaultRequestHeaders.Add("X-Octopus-Client", $"{RuntimeFeatureFlagEvaluator.ProductName} openfeature-provider-interim/1.0");
    }

    /// <inheritdoc />
    public override Metadata GetMetadata() => new("octopus-interim-provider");

    /// <summary>Reads the toggles once, then refreshes them every minute until shutdown.</summary>
    public override async Task InitializeAsync(EvaluationContext context, CancellationToken cancellationToken = default)
    {
        _evaluations = await ReadEvaluationsAsync(cancellationToken).ConfigureAwait(false) ?? [];
        _refreshing = RefreshAsync(_stopping.Token);
    }

    /// <inheritdoc />
    public override async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _stopping.CancelAsync().ConfigureAwait(false);
        await _refreshing.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey, bool defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Resolve(flagKey, defaultValue, context));

    /// <inheritdoc />
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey, string defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
        => Task.FromResult(NotBoolean(flagKey, defaultValue));

    /// <inheritdoc />
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey, int defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
        => Task.FromResult(NotBoolean(flagKey, defaultValue));

    /// <inheritdoc />
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey, double defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
        => Task.FromResult(NotBoolean(flagKey, defaultValue));

    /// <inheritdoc />
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey, Value defaultValue, EvaluationContext? context = null, CancellationToken cancellationToken = default)
        => Task.FromResult(NotBoolean(flagKey, defaultValue));

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
        _http.Dispose();
    }

    private ResolutionDetails<bool> Resolve(string flagKey, bool defaultValue, EvaluationContext? context)
    {
        var evaluation = Array.Find(_evaluations,
            candidate => string.Equals(candidate.Slug, flagKey, StringComparison.OrdinalIgnoreCase));
        if (evaluation is null)
        {
            return Failed(flagKey, defaultValue, ErrorType.FlagNotFound,
                "The slug provided did not match any of your Octopus Feature Flags.");
        }

        try
        {
            return evaluation.Resolve(context);
        }
        catch (FeatureProviderException ex)
        {
            return Failed(flagKey, defaultValue, ex.ErrorType, ex.Message);
        }
    }

    private static ResolutionDetails<T> NotBoolean<T>(string flagKey, T defaultValue)
        => Failed(flagKey, defaultValue, ErrorType.TypeMismatch, "Octopus only supports boolean flags.");

    private static ResolutionDetails<T> Failed<T>(string flagKey, T defaultValue, ErrorType error, string message)
        => new(flagKey, defaultValue, error, Reason.Error, errorMessage: message);

    private async Task RefreshAsync(CancellationToken stopping)
    {
        while (!stopping.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RefreshInterval, stopping).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (await ReadEvaluationsAsync(stopping).ConfigureAwait(false) is { } current)
            {
                _evaluations = current;
            }
        }
    }

    /// <summary>The toggles from Octopus, or null (logged) when they cannot be read, so the last good set is kept.</summary>
    private async Task<OctopusFlagEvaluation[]?> ReadEvaluationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(EvaluationsPath, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content
                .ReadFromJsonAsync(OctopusFlagsJsonContext.Default.OctopusFlagEvaluationArray, cancellationToken)
                .ConfigureAwait(false) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogError(ex, "Octopus feature toggles could not be read from {ServerUri}", _http.BaseAddress);
            return null;
        }
    }
}
