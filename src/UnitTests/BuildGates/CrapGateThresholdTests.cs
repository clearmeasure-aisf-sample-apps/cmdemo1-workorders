using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapGateThresholdTests
{
    [Test]
    public void ReadProductionThreshold_WhenConfigExists_ReturnsPositiveInteger()
    {
        var threshold = CrapGateThreshold.ReadProductionThreshold();

        threshold.ShouldBeGreaterThan(0);
    }

    [Test]
    public void EnforcementEntrypoints_WhenRead_DoNotHardcodeGateThresholdLiterals()
    {
        var threshold = CrapGateThreshold.ReadProductionThreshold();
        var privateBuild = RepositoryCheckout.Current.ReadText("PrivateBuild.ps1");
        var workflow = RepositoryCheckout.Current.ReadText(".github/workflows/build.yml");
        var auditScript = RepositoryCheckout.Current.ReadText("scripts/crap/run-crap-audit.ps1");
        var auditDoc = RepositoryCheckout.Current.ReadText("docs/crap-score-audit.md");
        var config = RepositoryCheckout.Current.ReadText(CrapGateThreshold.RelativeConfigPath);
        using var configDoc = System.Text.Json.JsonDocument.Parse(config);
        configDoc.RootElement.GetProperty("productionThreshold").GetInt32().ShouldBe(threshold);

        privateBuild.ShouldContain("crap-gate-threshold.json");
        privateBuild.ShouldNotContain($"-Threshold {threshold}");
        workflow.ShouldContain("crap-gate-threshold.json");
        workflow.ShouldNotContain($"-Threshold {threshold}");
        workflow.ShouldContain("name: Enforce CRAP (production)");
        auditScript.ShouldContain("crap-gate-threshold.json");
        auditScript.ShouldContain("Get-ProductionCrapGateThreshold");
        auditScript.ShouldNotContain($"[int]$Threshold = {threshold}");
        auditDoc.ShouldContain("crap-gate-threshold.json");
        auditDoc.ShouldContain("productionThreshold");
        auditDoc.ShouldNotContain($"-Threshold {threshold}");
    }

}
