using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class SizeAndNestingBaselineTests
{
    private const string BaselineRelativePath = "src/UnitTests/BuildGates/size-nesting-baseline.txt";

    [Test]
    public void ShouldReportUnexpected_WhenCurrentHasANewViolation()
    {
        var diff = SizeAndNestingBaseline.Diff(["file src/Core/A.cs"], []);

        diff.Unexpected.ShouldBe(["file src/Core/A.cs"]);
        diff.Stale.ShouldBeEmpty();
        diff.IsClean.ShouldBeFalse();
    }

    [Test]
    public void ShouldReportStale_WhenBaselineListsAFixedViolation()
    {
        var diff = SizeAndNestingBaseline.Diff([], ["file src/Core/A.cs"]);

        diff.Stale.ShouldBe(["file src/Core/A.cs"]);
        diff.Unexpected.ShouldBeEmpty();
        diff.IsClean.ShouldBeFalse();
    }

    [Test]
    public void ShouldPass_WhenScanMatchesBaseline()
    {
        var diff = SizeAndNestingBaseline.Diff(
            ["file src/Core/A.cs", "method src/Core/A.cs A.Run()"],
            ["method src/Core/A.cs A.Run()", "file src/Core/A.cs"]);

        diff.IsClean.ShouldBeTrue();
    }

    [Test]
    public void ShouldIgnoreCommentsAndBlankLines_WhenBaselineIsRead()
    {
        var path = Path.Combine(Path.GetTempPath(), "size-baseline-" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllText(path, "# comment\n\nfile src/Core/A.cs\n");

            SizeAndNestingBaseline.Read(path).ShouldBe(["file src/Core/A.cs"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ShouldFindDuplicateKeys_WhenBaselineRepeatsALine()
    {
        SizeAndNestingBaseline.DuplicateKeys(["b", "a", "b"]).ShouldBe(["b"]);
    }

    [Test]
    public void ShouldMatchRatchetBaseline_WhenRepositoryIsScanned()
    {
        var root = FindRepoRoot();
        var current = SizeAndNestingScanner.ScanRepository(root);
        var baselinePath = Path.Combine(root, BaselineRelativePath);
        var baseline = SizeAndNestingBaseline.Read(baselinePath);
        var duplicates = SizeAndNestingBaseline.DuplicateKeys(baseline);
        var diff = SizeAndNestingBaseline.Diff(current, baseline);

        duplicates.ShouldBeEmpty();
        diff.IsClean.ShouldBeTrue(SizeAndNestingBaseline.Format(diff));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PrivateBuild.ps1")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Repository root not found from the test directory.");
    }
}
