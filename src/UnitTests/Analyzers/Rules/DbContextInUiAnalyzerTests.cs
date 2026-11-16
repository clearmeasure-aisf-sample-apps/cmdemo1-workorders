using ClearMeasure.NpoAnalyzers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

[TestFixture]
public class DbContextInUiAnalyzerTests
{
    [Test]
    public async Task ShouldReportCm0005AsError_When_UiAssemblyDeclaresDbContextMember()
    {
        var source = DbContextInUiSamples.UiDeclaresDbContextMembers;

        var hits = await AnalyzerHarness.AnalyzeAsync(
            new DbContextInUiAnalyzer(),
            source,
            "ClearMeasure.Bootcamp.UI.Server");

        hits.ShouldContain(diagnostic =>
            diagnostic.Id == "CM0005" && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0005_When_AssemblyNameEndsWithUi()
    {
        var source = DbContextInUiSamples.SimpleDbContextMember;

        var hits = await AnalyzerHarness.AnalyzeAsync(
            new DbContextInUiAnalyzer(),
            source,
            "Contoso.UI");

        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0005");
    }

    [Test]
    public async Task ShouldReportNothing_When_DbContextIsOutsideUi()
    {
        var source = DbContextInUiSamples.RepositoryOutsideUi;

        var hits = await AnalyzerHarness.AnalyzeAsync(
            new DbContextInUiAnalyzer(),
            source,
            "ClearMeasure.Bootcamp.DataAccess");

        hits.Where(diagnostic => diagnostic.Id == "CM0005").ShouldBeEmpty();
    }
}
