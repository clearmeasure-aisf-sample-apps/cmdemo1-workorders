using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class WalkthroughGDocPresenceTests
{
    [Test]
    public void WalkthroughGDoc_WhenRead_ExistsInDocsFolder()
    {
        string path = FindRepoFile(Path.Combine("docs", "walkthrough-g.md"));
        File.Exists(path).ShouldBeTrue();
    }

    [Test]
    public void WalkthroughGDoc_WhenRead_HasH1Heading()
    {
        string path = FindRepoFile(Path.Combine("docs", "walkthrough-g.md"));
        File.ReadAllText(path).ShouldStartWith("# ");
    }

    [Test]
    public void WalkthroughGDoc_WhenRead_ContainsWalkthroughGTitle()
    {
        string path = FindRepoFile(Path.Combine("docs", "walkthrough-g.md"));
        File.ReadAllText(path).ShouldContain("Walkthrough G");
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
