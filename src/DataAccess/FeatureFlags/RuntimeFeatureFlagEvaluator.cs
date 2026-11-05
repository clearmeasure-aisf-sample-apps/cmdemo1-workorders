using System.Text;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octopus.OpenFeature.Provider;
using OpenFeature;
using OpenFeature.Constant;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>
/// Evaluates feature flags at runtime, first match wins:
/// <list type="number">
/// <item>Octopus Feature Toggles through OpenFeature, when <see cref="ClientIdentifierKey"/> is configured
/// (environment variable <c>Octopus__FeatureFlags__ClientIdentifier</c>). The toggle slug is the kebab-case flag
/// name (<c>building-management</c>) or the flag name itself, ignoring case.</item>
/// <item>The <see cref="ConfigurationSectionName"/> configuration section (<c>FeatureFlags:BuildingManagement</c>).</item>
/// <item>false.</item>
/// </list>
/// </summary>
public sealed class RuntimeFeatureFlagEvaluator : IFeatureFlagEvaluator
{
    /// <summary>Configuration key holding the Octopus Feature Toggles client identifier.</summary>
    public const string ClientIdentifierKey = "Octopus:FeatureFlags:ClientIdentifier";

    /// <summary>Configuration section holding per-flag fallback values.</summary>
    public const string ConfigurationSectionName = "FeatureFlags";

    private const string OpenFeatureDomain = "ChurchBulletin";

    private readonly IConfiguration _configuration;
    private readonly Lazy<Task<IFeatureClient?>> _client;

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public RuntimeFeatureFlagEvaluator(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _client = new Lazy<Task<IFeatureClient?>>(() => CreateOctopusClientAsync(configuration, loggerFactory));
    }

    /// <summary>Uses <paramref name="client"/> as the OpenFeature source instead of Octopus.</summary>
    internal RuntimeFeatureFlagEvaluator(IConfiguration configuration, IFeatureClient? client)
    {
        _configuration = configuration;
        _client = new Lazy<Task<IFeatureClient?>>(Task.FromResult(client));
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var client = await _client.Value.ConfigureAwait(false);
        if (client is not null)
        {
            foreach (var key in OpenFeatureKeys(featureName))
            {
                var details = await client
                    .GetBooleanDetailsAsync(key, false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (details.ErrorType == ErrorType.None)
                {
                    return details.Value;
                }
            }
        }

        return bool.TryParse(_configuration[$"{ConfigurationSectionName}:{featureName}"], out var enabled) && enabled;
    }

    /// <summary>The OpenFeature keys tried for <paramref name="featureName"/>, in order.</summary>
    internal static IEnumerable<string> OpenFeatureKeys(string featureName)
    {
        var slug = ToKebabCase(featureName);
        yield return slug;
        if (!string.Equals(slug, featureName, StringComparison.OrdinalIgnoreCase))
        {
            yield return featureName;
        }
    }

    private static string ToKebabCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 && !char.IsUpper(name[i - 1]))
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static async Task<IFeatureClient?> CreateOctopusClientAsync(
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var clientIdentifier = configuration[ClientIdentifierKey];
        if (string.IsNullOrWhiteSpace(clientIdentifier))
        {
            return null;
        }

        var logger = loggerFactory.CreateLogger<RuntimeFeatureFlagEvaluator>();
        try
        {
            var provider = new OctopusFeatureProvider(
                new OctopusFeatureConfiguration(clientIdentifier.Trim(), new ProductMetadata(OpenFeatureDomain))
                {
                    LoggerFactory = loggerFactory
                });
            await Api.Instance.SetProviderAsync(OpenFeatureDomain, provider).ConfigureAwait(false);
            return Api.Instance.GetClient(OpenFeatureDomain);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Octopus feature toggles are unavailable; using the {Section} configuration section",
                ConfigurationSectionName);
            return null;
        }
    }
}
