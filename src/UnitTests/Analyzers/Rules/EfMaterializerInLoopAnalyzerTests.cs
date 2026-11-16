using ClearMeasure.NpoAnalyzers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

[TestFixture]
public class EfMaterializerInLoopAnalyzerTests
{
    [Test]
    public async Task ShouldReportCm0002AsWarning_When_QueryableMaterializerIsInsideForeach()
    {
        var source = EfMaterializerInLoopSamples.QueryableMaterializerInsideForeach;

        var hits = await Cm0002Async(source);

        hits.Length.ShouldBe(4);
        hits.ShouldAllBe(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task ShouldReportCm0002_When_ReceiverImplementsIQueryable()
    {
        var source = EfMaterializerInLoopSamples.ReceiverImplementsIQueryable;

        var hits = await Cm0002Async(source);

        hits.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task ShouldReportNothing_When_InMemoryMaterializerIsInsideForeach()
    {
        var source = EfMaterializerInLoopSamples.InMemoryMaterializerInsideForeach;

        var hits = await Cm0002Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_SelectProjectsInMemoryArrays()
    {
        var source = EfMaterializerInLoopSamples.SelectProjectsInMemoryArrays;

        var hits = await Cm0002Async(source);

        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_MaterializerRunsOutsideALoop()
    {
        var source = EfMaterializerInLoopSamples.MaterializerOutsideLoop;

        var hits = await Cm0002Async(source);

        hits.ShouldBeEmpty();
    }

    private static async Task<Diagnostic[]> Cm0002Async(string source)
    {
        var diagnostics = await AnalyzerHarness.AnalyzeAsync(new EfMaterializerInLoopAnalyzer(), source);
        return diagnostics.Where(diagnostic => diagnostic.Id == "CM0002").ToArray();
    }
}
