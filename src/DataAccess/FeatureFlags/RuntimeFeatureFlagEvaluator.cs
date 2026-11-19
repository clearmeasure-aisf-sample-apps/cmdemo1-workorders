using System.Text;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octopus.OpenFeature.Provider;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>
/// Evaluates feature flags at runtime, first match wins:
/// <list type="number">
/// <item>Octopus Feature Toggles through OpenFeature, when <see cref="ClientIdentifierKey"/> is configured
/// (environment variable <c>Octopus__FeatureFlags__ClientIdentifier</c>). The toggle slug is tried as the
/// kebab-case flag name (<c>building-management</c>), then the compact lowercase Octopus slug
/// (<c>buildingmanagement</c>), then the flag name itself (<c>BuildingManagement</c>).</item>
/// <item>The <c>FeatureFlags</c> configuration section (<c>FeatureFlags:BuildingManagement</c>).</item>
/// <item>false.</item>
/// </list>
/// </summary>
public sealed class RuntimeFeatureFlagEvaluator : IFeatureFlagEvaluator
{
    /// <summary>Configuration key holding the Octopus Feature Toggles client identifier.</summary>
    public const string ClientIdentifierKey = "Octopus:FeatureFlags:ClientIdentifier";

    /// <summary>Configuration section holding per-flag fallback values.</summary>
    private const string ConfigurationSectionName = "FeatureFlags";

    private const string ProductName = "ChurchBulletin";

    private readonly IConfiguration _configuration;
    private readonly Lazy<Task<IFeatureClient?>> _client;

    /// <summary>Uses the Octopus OpenFeature provider when <see cref="ClientIdentifierKey"/> is configured.</summary>
    public RuntimeFeatureFlagEvaluator(IConfiguration configuration, ILoggerFactory loggerFactory)
        : this(configuration, loggerFactory, clientIdentifier => new OctopusFeatureProvider(
            new OctopusFeatureConfiguration(clientIdentifier, new ProductMetadata(ProductName))
            {
                LoggerFactory = loggerFactory
            }))
    {
    }

    /// <summary>Builds the OpenFeature provider with <paramref name="createProvider"/> instead of Octopus.</summary>
    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    internal RuntimeFeatureFlagEvaluator(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        Func<string, FeatureProvider> createProvider)
    {
        _configuration = configuration;
        _client = new Lazy<Task<IFeatureClient?>>(() => CreateClientAsync(
            configuration[ClientIdentifierKey],
            createProvider,
            loggerFactory.CreateLogger<RuntimeFeatureFlagEvaluator>()));
    }

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
        IsEnabledAsync(featureName, null, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(
        string featureName,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var client = await _client.Value.ConfigureAwait(false);
        if (client is not null)
        {
            var context = TargetingContext(userName);
            foreach (var key in OpenFeatureKeys(featureName))
            {
                var details = await client
                    .GetBooleanDetailsAsync(key, false, context: context, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (details.ErrorType == ErrorType.None)
                {
                    return details.Value;
                }
            }
        }

        return bool.TryParse(_configuration[$"{ConfigurationSectionName}:{featureName}"], out var enabled) && enabled;
    }

    /// <summary>
    /// The OpenFeature context for the signed-in user: the username as the targeting key and as the
    /// <c>username</c> attribute. An empty context when there is no username.
    /// </summary>
    private static EvaluationContext TargetingContext(string? userName)
    {
        var builder = EvaluationContext.Builder();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            builder = builder.SetTargetingKey(userName).Set("username", userName);
        }

        return builder.Build();
    }

    /// <summary>The OpenFeature keys tried for <paramref name="featureName"/>, in order.</summary>
    internal static IEnumerable<string> OpenFeatureKeys(string featureName)
    {
        var kebab = ToKebabCase(featureName);
        yield return kebab;

        var compact = kebab.Replace("-", string.Empty, StringComparison.Ordinal);
        if (!string.Equals(compact, kebab, StringComparison.Ordinal))
        {
            yield return compact;
        }

        if (!string.Equals(featureName, kebab, StringComparison.Ordinal)
            && !string.Equals(featureName, compact, StringComparison.Ordinal))
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

    private static async Task<IFeatureClient?> CreateClientAsync(
        string? clientIdentifier,
        Func<string, FeatureProvider> createProvider,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(clientIdentifier))
        {
            return null;
        }

        try
        {
            var domain = $"{ProductName}-{Guid.NewGuid():N}";
            await Api.Instance.SetProviderAsync(domain, createProvider(clientIdentifier.Trim())).ConfigureAwait(false);
            return Api.Instance.GetClient(domain);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Octopus feature toggles are unavailable; using the {Section} configuration section",
                ConfigurationSectionName);
            return null;
        }
    }
}
