using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class EnvironmentsDocPresenceTests
{
    [Test]
    public void EnvironmentsDoc_WhenRead_ExistsInDocsFolder()
    {
        string path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.Exists(path).ShouldBeTrue();
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_ContainsTddEnvironment()
    {
        string path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldContain("TDD");
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_ContainsUatEnvironment()
    {
        string path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldContain("UAT");
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_ContainsProductionEnvironment()
    {
        string path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldContain("Production");
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_HasH1Heading()
    {
        string path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldStartWith("# ");
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
