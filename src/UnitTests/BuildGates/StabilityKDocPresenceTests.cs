using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class StabilityKDocPresenceTests
{
    [Test]
    public void StabilityKDoc_WhenRead_ExistsInDocsFolder()
    {
        string path = FindRepoFile(Path.Combine("docs", "stability-k.md"));
        File.Exists(path).ShouldBeTrue();
    }

    [Test]
    public void StabilityKDoc_WhenRead_HasH1Heading()
    {
        string path = FindRepoFile(Path.Combine("docs", "stability-k.md"));
        File.ReadAllText(path).ShouldStartWith("# ");
    }

    [Test]
    public void StabilityKDoc_WhenRead_ContainsStabilityK()
    {
        string path = FindRepoFile(Path.Combine("docs", "stability-k.md"));
        File.ReadAllText(path).ShouldContain("Stability K");
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

        throw new FileNotFoundException($"Repo file not found: {relativePath}");
    }
}
