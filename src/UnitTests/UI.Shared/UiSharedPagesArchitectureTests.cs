using System.Text.RegularExpressions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

/// <summary>
/// Pages in UI.Shared reach domain work through <c>IBus</c>. HttpClient and DataContext stay out of those pages.
/// </summary>
[TestFixture]
public class UiSharedPagesArchitectureTests
{
    private static readonly Regex ForbiddenType = new(@"\b(HttpClient|DataContext)\b", RegexOptions.CultureInvariant);

    [Test]
    public void Pages_ShouldNotReferenceHttpClientOrDataContext_When_ScanningSource()
    {
        var pages = Path.Combine(FindRepoRoot(), "src", "UI.Shared", "Pages");
        var hits = Directory.EnumerateFiles(pages, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
            .Select(path => (Path: path, Match: ForbiddenType.Match(File.ReadAllText(path))))
            .Where(hit => hit.Match.Success)
            .Select(hit => $"{Path.GetRelativePath(pages, hit.Path)}:{hit.Match.Value}")
            .ToArray();

        hits.ShouldBeEmpty();
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
