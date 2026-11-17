using System.Net;
using System.Net.Http.Headers;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature.Constant;
using OpenFeature.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

[TestFixture]
public class OctopusFlagsProviderTests
{
    [Test]
    public async Task IsEnabledAsync_Should_TurnBuildingAndVendorManagementOn_When_OctopusHasThemOn()
    {
        var octopus = new StubOctopusTogglesHandler(OctopusTogglesMother.TddManagementFlagsOn);
        var evaluator = EvaluatorReading(octopus, ImageDefaultsWithManagementFlagsOff());

        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_UseConfiguration_When_OctopusIsUnavailable()
    {
        var octopus = new StubOctopusTogglesHandler(HttpStatusCode.ServiceUnavailable, string.Empty);
        var evaluator = EvaluatorReading(octopus, ImageDefaultsWithManagementFlagsOff());

        (await evaluator.IsEnabledAsync(FeatureFlagNames.BuildingManagement)).ShouldBeFalse();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.VendorManagement)).ShouldBeFalse();
        (await evaluator.IsEnabledAsync(FeatureFlagNames.ShowPriorityBadges)).ShouldBeTrue();
    }

    [Test]
    public async Task InitializeAsync_Should_ReadTheEvaluationsEndpointWithTheClientIdentifier()
    {
        var octopus = new StubOctopusTogglesHandler(OctopusTogglesMother.TddManagementFlagsOn);

        using var provider = await ProviderReading(octopus,
            Configuration((OctopusFlagsProvider.ServerUrlKey, "https://toggles.example.test/")));

        var request = octopus.Requests.ShouldHaveSingleItem();
        request.RequestUri.ShouldBe(new Uri("https://toggles.example.test/api/feature-flags/evaluations/v4/"));
        request.Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "client-123"));
    }

    [TestCase("eu-only", "eu", true, "Matched rule 'EU'.")]
    [TestCase("eu-only", "us", false, "Did not match any rules.")]
    [TestCase("outside-eu", "us", true, "Matched rule 'Outside EU'.")]
    [TestCase("outside-eu", "eu", false, "Did not match any rules.")]
    public async Task ResolveBooleanValueAsync_Should_ApplyContextAttributeRules_When_TheFlagDefersToTheClient(
        string slug, string region, bool expected, string reason)
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.EuRegionOnly));

        var details = await provider.ResolveBooleanValueAsync(slug, !expected, InRegion(region));

        details.Value.ShouldBe(expected);
        details.ErrorType.ShouldBe(ErrorType.None);
        details.Reason.ShouldBe(reason);
    }

    [Test]
    public async Task ResolveBooleanValueAsync_Should_TurnAFullRolloutOn_When_NoTargetingKeyIsSent()
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.EveryoneByPercentage));

        var details = await provider.ResolveBooleanValueAsync("everyone", false);

        details.Value.ShouldBeTrue();
        details.ErrorType.ShouldBe(ErrorType.None);
    }

    [Test]
    public async Task ResolveBooleanValueAsync_Should_ReportAnError_When_ARolloutNeedsATargetingKey()
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.EveryoneByPercentage));

        var details = await provider.ResolveBooleanValueAsync("everyone", false,
            EvaluationContext.Builder().SetTargetingKey("user-1").Build());

        details.Value.ShouldBeFalse();
        details.ErrorType.ShouldBe(ErrorType.General);
    }

    [Test]
    public async Task ResolveBooleanValueAsync_Should_NotMatch_When_TheConditionTypeIsNewerThanTheProvider()
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.ConditionFromANewerServer));

        var details = await provider.ResolveBooleanValueAsync("newer", true);

        details.Value.ShouldBeFalse();
        details.ErrorType.ShouldBe(ErrorType.None);
        details.Reason.ShouldBe("Did not match any rules.");
    }

    [Test]
    public async Task ResolveBooleanValueAsync_Should_ReportAParseError_When_AServerValueHasNoReason()
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.ValueWithoutReason));

        var details = await provider.ResolveBooleanValueAsync("malformed", false);

        details.Value.ShouldBeFalse();
        details.ErrorType.ShouldBe(ErrorType.ParseError);
    }

    [Test]
    public async Task ResolveBooleanValueAsync_Should_ReportFlagNotFound_When_TheSlugIsUnknown()
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.TddManagementFlagsOn));

        var details = await provider.ResolveBooleanValueAsync("not-a-flag", true);

        details.Value.ShouldBeTrue();
        details.ErrorType.ShouldBe(ErrorType.FlagNotFound);
    }

    [Test]
    public async Task ResolveStringValueAsync_Should_ReportATypeMismatch_When_AskedForANonBooleanValue()
    {
        using var provider = await ProviderReading(new StubOctopusTogglesHandler(OctopusTogglesMother.TddManagementFlagsOn));

        var details = await provider.ResolveStringValueAsync("building-management", "default");

        details.Value.ShouldBe("default");
        details.ErrorType.ShouldBe(ErrorType.TypeMismatch);
    }

    private static async Task<OctopusFlagsProvider> ProviderReading(
        StubOctopusTogglesHandler octopus, IConfiguration? configuration = null)
    {
        var provider = new OctopusFlagsProvider(
            "client-123", configuration ?? Configuration(), NullLogger.Instance, octopus);
        await provider.InitializeAsync(EvaluationContext.Empty);
        return provider;
    }

    private static RuntimeFeatureFlagEvaluator EvaluatorReading(StubOctopusTogglesHandler octopus, IConfiguration configuration) =>
        new(configuration, NullLoggerFactory.Instance, clientIdentifier =>
            new OctopusFlagsProvider(clientIdentifier, configuration, NullLogger.Instance, octopus));

    /// <summary>The image's appsettings.json: management flags off, badges on, plus the TDD client identifier.</summary>
    private static IConfiguration ImageDefaultsWithManagementFlagsOff() => Configuration(
        (RuntimeFeatureFlagEvaluator.ClientIdentifierKey, "client-123"),
        ("FeatureFlags:BuildingManagement", "false"),
        ("FeatureFlags:VendorManagement", "false"),
        ("FeatureFlags:ShowPriorityBadges", "true"));

    private static EvaluationContext InRegion(string region) => EvaluationContext.Builder().Set("region", region).Build();

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}
