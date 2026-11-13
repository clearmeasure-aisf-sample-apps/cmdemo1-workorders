using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>Source-level checks that App and Authentication fixtures stay API-seeded and ParallelScope.All.</summary>
[TestFixture]
public class AppAuthFixtureIndependenceTests
{
    public static IEnumerable<string> AppAndAuthFixturePaths()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.SourceFiles(".cs")
            .Select(checkout.Relative)
            .Where(path =>
                path.StartsWith("src/AcceptanceTests/App/", StringComparison.Ordinal)
                || path.StartsWith("src/AcceptanceTests/Authentication/", StringComparison.Ordinal))
            .Where(path => path.EndsWith("Tests.cs", StringComparison.Ordinal));
    }

    [Test]
    public void AcceptanceApiTestBase_WhenRead_SeedsThroughObjectMother()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/AcceptanceApiTestBase.cs");
        source.ShouldContain("class AcceptanceApiTestBase : AcceptanceTestBase");
        source.ShouldContain("ParallelScope.All");
        source.ShouldContain("AcceptanceObjectMother");
        source.ShouldContain("SeedEmployeeAsync");
        source.ShouldContain("SeedWorkOrderAsync");
        source.ShouldNotContain("NewDbContext");
    }

    [TestCaseSource(nameof(AppAndAuthFixturePaths))]
    public void AppOrAuthFixture_WhenRead_DoesNotUseDataContext(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        text.ShouldNotContain("NewDbContext");
        text.ShouldNotContain("SaveChangesAsync");
    }

    [TestCaseSource(nameof(AppAndAuthFixturePaths))]
    public void AppOrAuthFixture_WhenRead_IsParallelScopeAll(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        var inheritsApiBase = text.Contains("AppAcceptanceTestBase", StringComparison.Ordinal)
            || text.Contains("AuthAcceptanceTestBase", StringComparison.Ordinal);
        var declaresAll = text.Contains("ParallelScope.All", StringComparison.Ordinal);
        (inheritsApiBase || declaresAll).ShouldBeTrue(path);
        text.ShouldNotContain("NonParallelizable");
        text.ShouldNotContain("SerialLane");
    }
}
