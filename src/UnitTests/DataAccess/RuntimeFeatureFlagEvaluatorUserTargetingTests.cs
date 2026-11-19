using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpenFeature;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

/// <summary>
/// User targeting of runtime feature flags: the evaluator sends the signed-in username to OpenFeature as the
/// targeting key and the <c>username</c> attribute, so a provider can roll a flag out to one person first.
/// </summary>
[TestFixture]
public class RuntimeFeatureFlagEvaluatorUserTargetingTests
{
    [Test]
    public async Task IsEnabledAsync_Should_ReturnTrueForRolloutUser_When_LaterPhaseIsOff()
    {
        var evaluator = WithTargetingProvider(new TargetingStubFeatureProvider());

        (await evaluator.IsEnabledAsync(
            FeatureFlagNames.EarlyAccessBanner, TargetingStubFeatureProvider.EarlyAccessUser)).ShouldBeTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnFalseForOtherUser_When_LaterPhaseIsOff()
    {
        var evaluator = WithTargetingProvider(new TargetingStubFeatureProvider());

        (await evaluator.IsEnabledAsync(
            FeatureFlagNames.EarlyAccessBanner, TargetingStubFeatureProvider.OtherUser)).ShouldBeFalse();
    }

    [Test]
    public async Task IsEnabledAsync_Should_ReturnTrueForEveryone_When_LaterPhaseIsOn()
    {
        var evaluator = WithTargetingProvider(new TargetingStubFeatureProvider { LaterPhase = true });

        (await evaluator.IsEnabledAsync(
            FeatureFlagNames.EarlyAccessBanner, TargetingStubFeatureProvider.EarlyAccessUser)).ShouldBeTrue();
        (await evaluator.IsEnabledAsync(
            FeatureFlagNames.EarlyAccessBanner, TargetingStubFeatureProvider.OtherUser)).ShouldBeTrue();
    }

    [Test]
    public async Task IsEnabledAsync_Should_SendUsernameAsTargetingKeyAndAttribute_When_UserNameIsGiven()
    {
        var provider = new TargetingStubFeatureProvider();
        var evaluator = WithTargetingProvider(provider);

        await evaluator.IsEnabledAsync(
            FeatureFlagNames.EarlyAccessBanner, TargetingStubFeatureProvider.EarlyAccessUser);

        var context = provider.Contexts.Single();
        context.TargetingKey.ShouldBe(TargetingStubFeatureProvider.EarlyAccessUser);
        context.GetValue("username").AsString.ShouldBe(context.TargetingKey);
    }

    private static RuntimeFeatureFlagEvaluator WithTargetingProvider(FeatureProvider provider) =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [RuntimeFeatureFlagEvaluator.ClientIdentifierKey] = "client-123"
                })
                .Build(),
            NullLoggerFactory.Instance,
            _ => provider);
}
