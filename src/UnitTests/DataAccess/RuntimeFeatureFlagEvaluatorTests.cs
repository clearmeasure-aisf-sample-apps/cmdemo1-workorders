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
    public async Task IsEnabledAsync_Should_PreferOpenFeature_When_ProviderHasTheFlag()
    {
        var client = await InMemoryClientAsync(("building-management", true), ("vendor-management", false));
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration(("FeatureFlags:BuildingManagement", "false"), ("FeatureFlags:VendorManagement", "true")),
            client);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_MatchFlagNameAsSlug_When_KebabSlugIsMissing()
    {
        var client = await InMemoryClientAsync((FeatureFlagNames.VendorManagement, true));
        var evaluator = new RuntimeFeatureFlagEvaluator(Configuration(), client);

        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_FallBackToConfiguration_When_ProviderLacksTheFlag()
    {
        var client = await InMemoryClientAsync(("building-management", true));
        var evaluator = new RuntimeFeatureFlagEvaluator(
            Configuration(("FeatureFlags:ShowPriorityBadges", "true")), client);

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

    private static async Task<IFeatureClient> InMemoryClientAsync(params (string Key, bool Enabled)[] flags)
    {
        var domain = $"unit-{Guid.NewGuid():N}";
        var provider = new InMemoryProvider(flags.ToDictionary(
            flag => flag.Key,
            Flag (flag) => new Flag<bool>(
                new Dictionary<string, bool> { ["on"] = true, ["off"] = false },
                flag.Enabled ? "on" : "off")));
        await OpenFeature.Api.Instance.SetProviderAsync(domain, provider);
        return OpenFeature.Api.Instance.GetClient(domain);
    }
}
