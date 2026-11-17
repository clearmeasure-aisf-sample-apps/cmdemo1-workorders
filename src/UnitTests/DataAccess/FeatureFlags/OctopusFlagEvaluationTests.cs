using OpenFeature.Constant;
using OpenFeature.Error;
using Shouldly;
using static ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags.OctopusFlagMother;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

[TestFixture]
public class OctopusFlagEvaluationTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Resolve_Should_ReturnTheServerValueAndReason_When_TheServerResolvedTheToggle(bool value)
    {
        var details = ServerResolved(value, "Resolved by Octopus.").Resolve(null);

        details.Value.ShouldBe(value);
        details.Reason.ShouldBe("Resolved by Octopus.");
        details.ErrorType.ShouldBe(ErrorType.None);
    }

    [Test]
    public void Resolve_Should_TurnOnAtTheFirstMatchingRule_When_TheToggleIsDeferred()
    {
        var toggle = Deferred(Rule("UK", IsOneOf("region", "uk")), Rule("EU", IsOneOf("region", "eu")));

        toggle.Resolve(InRegion("eu")).Reason.ShouldBe("Matched rule 'EU'.");
        toggle.Resolve(InRegion("us")).Reason.ShouldBe("Did not match any rules.");
        toggle.Resolve(InRegion("us")).Value.ShouldBeFalse();
    }

    [Test]
    public void Resolve_Should_ThrowParseErrors_When_AServerResolvedToggleIsMalformed()
    {
        Should.Throw<ParseErrorException>(() => ServerResolved(true, reason: null).Resolve(null));
        Should.Throw<ParseErrorException>(() => (ServerResolved(true) with { EvaluationKey = "k" }).Resolve(null));
        Should.Throw<ParseErrorException>(() => (ServerResolved(true) with { Rules = [] }).Resolve(null));
    }

    [Test]
    public void Resolve_Should_ThrowParseErrors_When_ADeferredToggleIsMalformed()
    {
        Should.Throw<ParseErrorException>(() => Deferred(null).Resolve(null));
        Should.Throw<ParseErrorException>(() => (Deferred(Rule("R", Percentage(100))) with { EvaluationKey = null }).Resolve(null));
        Should.Throw<ParseErrorException>(() => Deferred().Resolve(null));
        Should.Throw<ParseErrorException>(() => Deferred([null]).Resolve(null));
    }
}
