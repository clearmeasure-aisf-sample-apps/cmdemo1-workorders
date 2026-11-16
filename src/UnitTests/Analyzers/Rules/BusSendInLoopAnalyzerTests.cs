using ClearMeasure.NpoAnalyzers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

[TestFixture]
public class BusSendInLoopAnalyzerTests
{
    [Test]
    public async Task ShouldReportCm0001AsError_When_TeamsForeachAwaitsBusSend()
    {
        var source = BusSendInLoopSamples.TeamsForeachAwaitsBusSend;

        var hits = await Cm0001Async(source);

        hits.Length.ShouldBe(1);
        hits[0].Id.ShouldBe("CM0001");
        hits[0].Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportNothing_When_SingleSendIsOutsideALoop()
    {
        var source = BusSendInLoopSamples.SingleSendOutsideLoop;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ForeachAwaitsFieldNamedBus()
    {
        var source = BusSendInLoopSamples.ForeachAwaitsFieldNamedBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0001_When_SelectLambdaAwaitsBusSend()
    {
        var source = BusSendInLoopSamples.SelectLambdaAwaitsBusSend;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem().Id.ShouldBe("CM0001");
    }

    [Test]
    public async Task ShouldReportNothing_When_SendOnlyEnumeratesTheForeach()
    {
        var source = BusSendInLoopSamples.SendOnlyEnumeratesForeach;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInTheForCondition()
    {
        var source = BusSendInLoopSamples.SendInForCondition;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInTheWhileCondition()
    {
        var source = BusSendInLoopSamples.SendInWhileCondition;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInTheDoWhileCondition()
    {
        var source = BusSendInLoopSamples.SendInDoWhileCondition;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInTheForIncrementor()
    {
        var source = BusSendInLoopSamples.SendInForIncrementor;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportNothing_When_LocalSendIsInsideForeach()
    {
        var source = BusSendInLoopSamples.LocalSendInsideForeach;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInsideForBody()
    {
        var source = BusSendInLoopSamples.SendInsideForBody;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInsideWhileBody()
    {
        var source = BusSendInLoopSamples.SendInsideWhileBody;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInsideDoWhileBody()
    {
        var source = BusSendInLoopSamples.SendInsideDoWhileBody;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ConfigureAwaitSendIsInsideForeach()
    {
        var source = BusSendInLoopSamples.ConfigureAwaitSendInsideForeach;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportNothing_When_HttpClientSendAsyncIsInsideForeach()
    {
        var source = BusSendInLoopSamples.HttpClientSendAsyncInsideForeach;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_UnrelatedSendIsInsideForeach()
    {
        var source = BusSendInLoopSamples.UnrelatedSendInsideForeach;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ConcreteBusImplementsIBus()
    {
        var source = BusSendInLoopSamples.ConcreteBusImplementsIBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ISenderReceiverIsNotNamedBus()
    {
        var source = BusSendInLoopSamples.SenderReceiverNotNamedBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0001_When_LoopAwaitsBlockHelperThatAwaitsBus()
    {
        var source = BusSendInLoopFollowSamples.BlockHelperAwaitsBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0001_When_LoopAwaitsExpressionHelperThatAwaitsBus()
    {
        var source = BusSendInLoopFollowSamples.ExpressionHelperAwaitsBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_LoopAwaitsLocalFunctionThatAwaitsBus()
    {
        var source = BusSendInLoopFollowSamples.LocalFunctionAwaitsBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SelectAwaitsHelperThatAwaitsBus()
    {
        var source = BusSendInLoopFollowSamples.SelectHelperAwaitsBus;

        var hits = await Cm0001Async(source);

        hits.ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportNothing_When_HelperHasNoBusSend()
    {
        var source = BusSendInLoopFollowSamples.HelperWithNoBus;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_HelperLivesOnOtherType()
    {
        var source = BusSendInLoopFollowSamples.HelperOnOtherType;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_TwoLevelsBeforeBusSend()
    {
        var source = BusSendInLoopFollowSamples.TwoLevelsBeforeBus;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_NestedLocalHoldsBusSend()
    {
        var source = BusSendInLoopFollowSamples.NestedLocalHoldsBus;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_HelperReturnsSendWithoutAwait()
    {
        var source = BusSendInLoopFollowSamples.HelperReturnsSend;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_HelperThatAwaitsBusIsOutsideLoop()
    {
        var source = BusSendInLoopFollowSamples.HelperOutsideLoop;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_BusSendIsReachedThroughDelegate()
    {
        var source = BusSendInLoopFollowSamples.ThroughDelegate;

        var hits = await Cm0001Async(source);

        hits.ShouldBeEmpty();
    }

    private static async Task<Diagnostic[]> Cm0001Async(string source)
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(new BusSendInLoopAnalyzer(), source);
        return diagnostics.Where(diagnostic => diagnostic.Id == "CM0001").ToArray();
    }
}
