using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class SuperfluousStructureGateTests
{
    public const string AllowListPath =
        "src/UnitTests/BuildGates/SuperfluousStructure/superfluous-structure-allowlist.json";

    public const string BaselinePath =
        "src/UnitTests/BuildGates/SuperfluousStructure/superfluous-structure-baseline.json";

    [Test]
    public void ShouldMatchBaseline_When_RepositoryIsScanned()
    {
        var root = FindRepoRoot();
        var report = SuperfluousStructureScanner.Scan(root);
        var allowList = StructureAllowList.Load(File.ReadAllText(Path.Combine(root, AllowListPath)));
        var baseline = StructureBaseline.Load(File.ReadAllText(Path.Combine(root, BaselinePath)));
        foreach (var rule in StructureRule.All)
        {
            foreach (var id in allowList.Ids(rule))
            {
                allowList.Reason(rule, id).ShouldNotBeNullOrWhiteSpace();
            }
        }

        var problems = StructureRatchet.Evaluate(report, allowList, baseline);
        problems.ShouldBeEmpty(string.Join(Environment.NewLine, problems));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}
