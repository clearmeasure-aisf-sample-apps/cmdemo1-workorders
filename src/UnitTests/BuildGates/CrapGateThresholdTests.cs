using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapGateThresholdTests
{
    [Test]
    public void ReadProductionThreshold_WhenConfigExists_ReturnsPositiveInteger()
    {
        int threshold = CrapGateThreshold.ReadProductionThreshold();

        threshold.ShouldBeGreaterThan(0);
    }

    [Test]
    public void EnforcementEntrypoints_WhenRead_DoNotHardcodeGateThresholdLiterals()
    {
        int threshold = CrapGateThreshold.ReadProductionThreshold();
        string privateBuild = File.ReadAllText(FindRepoFile("PrivateBuild.ps1"));
        string workflow = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));
        string auditScript = File.ReadAllText(FindRepoFile(Path.Combine(
            "scripts", "crap", "run-crap-audit.ps1")));
        string auditDoc = File.ReadAllText(FindRepoFile(Path.Combine(
            "docs", "crap-score-audit.md")));
        string config = File.ReadAllText(FindRepoFile(CrapGateThreshold.RelativeConfigPath));
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

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"{relativePath} not found from test directory.");
    }
}
