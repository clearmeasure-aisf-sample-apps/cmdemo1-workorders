using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
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
        var pages = RepositoryCheckout.Current.PathOf("src/UI.Shared/Pages");
        var hits = Directory.EnumerateFiles(pages, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
            .Select(path => (Path: path, Match: ForbiddenType.Match(File.ReadAllText(path))))
            .Where(hit => hit.Match.Success)
            .Select(hit => $"{Path.GetRelativePath(pages, hit.Path)}:{hit.Match.Value}")
            .ToArray();

        hits.ShouldBeEmpty();
    }

}
