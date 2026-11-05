using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using OpenFeature.Providers.Memory;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class RuntimeFeatureFlagEvaluatorTests
{
    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_NoProviderKnowsTheFlag()
    {
        var evaluator = new RuntimeFeatureFlagEvaluator(Configuration(), NullLoggerFactory.Instance);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeFalse();
        (await evaluator.IsEnabledAsync("NotInTheCatalog")).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_UseConfiguration_When_ClientIdentifierIsMissing()
    {
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration(("FeatureFlags:VendorManagement", "true"), ("FeatureFlags:BuildingManagement", "false")),
            NullLoggerFactory.Instance);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalse_When_ConfigurationValueIsNotBoolean()
    {
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration(("FeatureFlags:VendorManagement", "yes")), NullLoggerFactory.Instance);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_PreferOpenFeature_When_ClientIdentifierIsConfigured()
    {
        var factory = new StubProviderFactory(("building-management", true), ("vendor-management", false));
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration(
                (RuntimeFeatureFlagEvaluator.ClientIdentifierKey, "  client-123  "),
                ("FeatureFlags:BuildingManagement", "false"),
                ("FeatureFlags:VendorManagement", "true")),
            NullLoggerFactory.Instance,
            factory.Create);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
        factory.ClientIdentifiers.ShouldBe(["client-123"]);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task IsEnabledAsync_Should_SkipOpenFeature_When_ClientIdentifierIsBlank(string clientIdentifier)
    {
        var factory = new StubProviderFactory(("vendor-management", true));
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration((RuntimeFeatureFlagEvaluator.ClientIdentifierKey, clientIdentifier)),
            NullLoggerFactory.Instance,
            factory.Create);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
        factory.ClientIdentifiers.ShouldBeEmpty();
    }

    [Test]
    public async Task IsEnabledAsync_Should_UseConfiguration_When_ProviderCannotBeCreated()
    {
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration(
                (RuntimeFeatureFlagEvaluator.ClientIdentifierKey, "client-123"),
                ("FeatureFlags:VendorManagement", "true")),
            NullLoggerFactory.Instance,
            _ => throw new InvalidOperationException("unreachable"));

        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_MatchFlagNameAsSlug_When_KebabSlugIsMissing()
    {
        var evaluator = WithOpenFeature(Configuration(), (FeatureFlagNames.VendorManagement, true));

        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_FallBackToConfiguration_When_ProviderLacksTheFlag()
    {
        var evaluator = WithOpenFeature(
            Configuration(("FeatureFlags:ShowPriorityBadges", "true")), ("building-management", true));

        (await evaluator.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
    }

    [TestCase("BuildingManagement", new[] { "building-management", "BuildingManagement" })]
    [TestCase("ShowPriorityBadges", new[] { "show-priority-badges", "ShowPriorityBadges" })]
    [TestCase("SampleFeatureA", new[] { "sample-feature-a", "SampleFeatureA" })]
    [TestCase("lowercase", new[] { "lowercase" })]
    public void OpenFeatureKeys_Should_TryKebabSlugThenName_When_Evaluating(string name, string[] expected)
    {
        RuntimeFeatureFlagEvaluator.OpenFeatureKeys(name).ShouldBe(expected);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static RuntimeFeatureFlagEvaluator WithOpenFeature(
        IConfiguration fallback,
        params (string Key, bool Enabled)[] flags)
    {
        var configuration = new ConfigurationBuilder()
            .AddConfiguration(fallback)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RuntimeFeatureFlagEvaluator.ClientIdentifierKey] = "client-123"
            })
            .Build();
        return new RuntimeFeatureFlagEvaluator(configuration, NullLoggerFactory.Instance,
            new StubProviderFactory(flags).Create);
    }

    private sealed class StubProviderFactory(params (string Key, bool Enabled)[] flags)
    {
        public List<string> ClientIdentifiers { get; } = [];

        public FeatureProvider Create(string clientIdentifier)
        {
            ClientIdentifiers.Add(clientIdentifier);
            return new InMemoryProvider(flags.ToDictionary(
                flag => flag.Key,
                Flag (flag) => new Flag<bool>(
                    new Dictionary<string, bool> { ["on"] = true, ["off"] = false },
                    flag.Enabled ? "on" : "off")));
        }
    }
}
