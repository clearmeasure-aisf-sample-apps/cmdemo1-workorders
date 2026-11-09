using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapGateEvaluatorTests
{
    [Test]
    public void EvaluateReport_WhenOnlyTestsAndGeneratedExceedThreshold_ReturnsZero()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var result = CrapGateEvaluator.EvaluateReport(PassFixture(gate), threshold: gate);

        result.ViolationCount.ShouldBe(0);
        result.Methods.ShouldBeEmpty();
    }

    [Test]
    public void EvaluateReport_WhenProductionMethodExceedsThreshold_ReturnsThatMethod()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();

        var (threshold, violationCount, methods) = CrapGateEvaluator.EvaluateReport(FailFixture(gate), threshold: gate);

        threshold.ShouldBe(gate);
        violationCount.ShouldBe(1);
        ShouldBeTheCsvParser(methods.Single());
    }

    [Test]
    public void EvaluateReport_WhenWindowsBackslashProductionPathExceedsThreshold_CountsViolation()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var json = $$"""
            {
              "threshold": {{gate}},
              "methods": [
                {
                  "fullName": "ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderCommandExecutor.ExecuteCommandAsync",
                  "filePath": "D:\\\\bootcamp-palermo-workorders\\\\src\\\\McpServer\\\\Tools\\\\WorkOrderCommandExecutor.cs",
                  "crap": 56,
                  "complexity": 7,
                  "coverage": 0
                }
              ]
            }
            """;

        CrapGateEvaluator.EvaluateReport(json, threshold: gate).ViolationCount.ShouldBe(1);
    }

    [Test]
    public void EvaluateReport_WhenThresholdRaisedAboveProductionCrap_ReturnsZero()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        CrapGateEvaluator.EvaluateReport(FailFixture(gate), threshold: 20).ViolationCount.ShouldBe(0);
    }

    [Test]
    public void EvaluateReport_WhenProductionCrapIsJustOverGate_RejectsAtGateAndAcceptsAtGatePlusOne()
    {
        var gate = CrapGateThreshold.ReadProductionThreshold();
        var justOver = gate + 1;
        var json = $$"""
            {
              "threshold": {{gate}},
              "methods": [
                {
                  "fullName": "ClearMeasure.Bootcamp.Core.Model.WorkOrder.ChangeStatus",
                  "filePath": "D:/repo/src/Core/Model/WorkOrder.cs",
                  "crap": {{justOver}},
                  "complexity": {{justOver}},
                  "coverage": 100
                }
              ]
            }
            """;

        CrapGateEvaluator.EvaluateReport(json, threshold: gate).ViolationCount.ShouldBe(1);
        CrapGateEvaluator.EvaluateReport(json, threshold: justOver).ViolationCount.ShouldBe(0);
    }

    [Test]
    public void RollupScript_WhenRead_ContainsProductionExclusionTokens()
    {
        var source = RepositoryCheckout.Current.ReadText("scripts/crap/rollup-file-scores.csx");

        source.ShouldContain("IsProductionFile");
        source.ShouldContain("NormalizePath(path)");
        source.ShouldContain("/unittests/");
        source.ShouldContain("/integrationtests/");
        source.ShouldContain("/acceptancetests/");
        source.ShouldContain("/generated/");
        source.ShouldContain(".g.cs");
        source.ShouldContain(".designer.cs");
        source.ShouldContain("/src/");
        source.ShouldContain("crap-production-violations.json");
        source.ShouldContain("OverlayLineCoverage");
        source.ShouldContain("coverage.flattened.cobertura.xml");
    }

    [Test]
    public void RollupScript_WriteSummary_UsesProductionGateStatsNotWholeSolutionStats()
    {
        var source = RepositoryCheckout.Current.ReadText("scripts/crap/rollup-file-scores.csx");
        var writeSummaryStart = source.IndexOf("static async Task WriteSummaryAsync", StringComparison.Ordinal);
        writeSummaryStart.ShouldBeGreaterThan(-1);
        var writeSummaryEnd = source.IndexOf("static double Median", writeSummaryStart, StringComparison.Ordinal);
        writeSummaryEnd.ShouldBeGreaterThan(writeSummaryStart);
        var writeSummary = source.Substring(writeSummaryStart, writeSummaryEnd - writeSummaryStart);

        writeSummary.ShouldContain("## Production gate stats");
        writeSummary.ShouldContain("Out-of-scope paths");
        writeSummary.ShouldContain("IsProductionFile(m.FilePath)");
        writeSummary.ShouldNotContain("## Solution stats");
        writeSummary.ShouldNotContain("report.Stats.MethodCount");
        writeSummary.ShouldNotContain("report.Stats.CrappyMethodCount");
        writeSummary.ShouldNotContain("report.Stats.AverageCrap");
        writeSummary.ShouldNotContain("report.Stats.MedianCrap");
        writeSummary.ShouldNotContain("report.Stats.TotalCrapLoad");
    }

    [Test]
    public void AuditScript_WhenRead_PrependsDotnetToolsToPath()
    {
        var source = RepositoryCheckout.Current.ReadText("scripts/crap/run-crap-audit.ps1");

        source.ShouldContain("Join-Path $HOME \".dotnet\" \"tools\"");
        source.ShouldContain("$env:PATH = \"$dotnetTools");
        source.ShouldContain("dotnet-script\" -Command \"dotnet-script\" -Version \"2.0.0\"");
        source.ShouldContain("Failed to roll up CRAP scores");
    }

    [Test]
    public void FlattenScript_WhenRead_ContainsAsyncStateMachinePattern()
    {
        var source = RepositoryCheckout.Current.ReadText("scripts/crap/flatten-cobertura.csx");

        source.ShouldContain("StateMachineClassName");
        source.ShouldContain("FlattenAndMerge");
        source.ShouldContain("<(?<method>[^>]+)>d__");
    }

    private static void ShouldBeTheCsvParser(CrapMethodViolation violation)
    {
        var (fullName, filePath, (crap, complexity, coverage)) = violation;
        fullName.ShouldBe("ClearMeasure.Bootcamp.Core.Import.WorkOrderBulkImportCsvParser.Parse");
        filePath.ShouldBe("D:/repo/src/Core/Import/WorkOrderBulkImportCsvParser.cs");
        crap.ShouldBe(20);
        complexity.ShouldBe(4);
        coverage.ShouldBe(0);
    }

    private static string PassFixture(int threshold) => $$"""
        {
          "threshold": {{threshold}},
          "methods": [
            {
              "fullName": "ClearMeasure.Bootcamp.Core.Model.WorkOrder.get_Title",
              "filePath": "D:/repo/src/Core/Model/WorkOrder.cs",
              "crap": 2,
              "complexity": 1,
              "coverage": 100
            },
            {
              "fullName": "ClearMeasure.Bootcamp.UnitTests.Foo.Bar",
              "filePath": "D:/repo/src/UnitTests/Foo.cs",
              "crap": 420,
              "complexity": 20,
              "coverage": 0
            },
            {
              "fullName": "ClearMeasure.Bootcamp.UI.Server.Generated.Workorders.MergeFrom",
              "filePath": "D:/repo/src/UI/Server/Generated/Protos/Workorders.cs",
              "crap": 272,
              "complexity": 16,
              "coverage": 0
            }
          ]
        }
        """;

    private static string FailFixture(int threshold) => $$"""
        {
          "threshold": {{threshold}},
          "methods": [
            {
              "fullName": "ClearMeasure.Bootcamp.Core.Import.WorkOrderBulkImportCsvParser.Parse",
              "filePath": "D:/repo/src/Core/Import/WorkOrderBulkImportCsvParser.cs",
              "crap": 20,
              "complexity": 4,
              "coverage": 0
            },
            {
              "fullName": "ClearMeasure.Bootcamp.AcceptanceTests.ToTestDateTime",
              "filePath": "D:/repo/src/AcceptanceTests/Extensions/DateTimeTestExtensions.cs",
              "crap": 420,
              "complexity": 20,
              "coverage": 0
            }
          ]
        }
        """;
}
