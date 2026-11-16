using ClearMeasure.NpoAnalyzers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

[TestFixture]
public class HttpInLoopAnalyzerTests
{
    [Test]
    public async Task ShouldReportCm0003_When_HttpGetIsInsideForeach()
    {
        var source = HttpInLoopSamples.HttpGetInsideForeach;

        var hits = await AnalyzerHarness.AnalyzeAsync(new HttpInLoopAnalyzer(), source);

        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0003");
    }

    [Test]
    public async Task ShouldReportNothing_When_HttpGetRunsOutsideALoop()
    {
        var source = HttpInLoopSamples.HttpGetOutsideLoop;

        var hits = await AnalyzerHarness.AnalyzeAsync(new HttpInLoopAnalyzer(), source);

        hits.Where(diagnostic => diagnostic.Id == "CM0003").ShouldBeEmpty();
    }
}
