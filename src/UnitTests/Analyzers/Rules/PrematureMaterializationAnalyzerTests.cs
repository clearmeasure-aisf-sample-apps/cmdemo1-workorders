using ClearMeasure.NpoAnalyzers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

[TestFixture]
public class PrematureMaterializationAnalyzerTests
{
    [Test]
    public async Task ShouldReportCm0004_When_ToListPrecedesWhere()
    {
        var source = PrematureMaterializationSamples.ToListPrecedesWhere;

        var hits = await AnalyzerHarness.AnalyzeAsync(new PrematureMaterializationAnalyzer(), source);

        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0004");
    }

    [Test]
    public async Task ShouldReportNothing_When_WhereToListRunsOutsideALoop()
    {
        var source = PrematureMaterializationSamples.WhereToListOutsideLoop;

        var hits = await AnalyzerHarness.AnalyzeAsync(new PrematureMaterializationAnalyzer(), source);

        hits.Where(diagnostic => diagnostic.Id == "CM0004").ShouldBeEmpty();
    }
}
