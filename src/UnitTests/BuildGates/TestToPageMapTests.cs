using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class TestToPageMapTests
{
    [Test]
    public void Evaluate_WhenTheCommittedMapMatchesPageLevelFixtures_HasNoProblems()
    {
        var checkout = RepositoryCheckout.Current;
        var map = TestToPageMap.Load(checkout);

        var problems = TestToPageMapCoverage.Evaluate(map, PageLevelAcceptanceCatalog.Read(checkout), checkout);

        problems.ShouldBeEmpty();
    }

    [Test]
    public void TestsForRazorPaths_WhenAPageChanges_ReturnsMappedFixtures()
    {
        var map = SampleMap();

        var selected = map.TestsForRazorPaths(["src/UI.Shared/Pages/Login.razor"]);

        selected.ShouldBe(
        [
            "ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests",
        ]);
    }

    [Test]
    public void TestsForRazorPaths_WhenCodeBehindChanges_MatchesTheRazorPage()
    {
        var map = SampleMap();

        var selected = map.TestsForRazorPaths(["src/UI.Shared/Pages/Login.razor.cs"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests");
    }

    [Test]
    public void TestsForRazorPaths_WhenUnmappedRazorChanges_ReturnsEmpty()
    {
        var map = SampleMap();

        map.TestsForRazorPaths(["src/UI.Shared/Pages/Reports.razor"]).ShouldBeEmpty();
    }

    [Test]
    public void Evaluate_WhenAPageLevelFixtureIsMissing_NamesThatFixture()
    {
        var checkout = RepositoryCheckout.Current;
        var map = new TestToPageMap
        {
            Tests =
            [
                new TestToPageMapEntry
                {
                    Class = "Only.One",
                    Pages = ["src/UI.Shared/Pages/Index.razor"],
                },
            ],
        };
        var pageLevel = new[]
        {
            new DiscoveredAcceptanceClass("Only.One", 1),
            new DiscoveredAcceptanceClass("Missing.Fixture", 1),
        };

        var problems = TestToPageMapCoverage.Evaluate(map, pageLevel, checkout);

        string.Join('\n', problems).ShouldContain("Missing.Fixture");
        string.Join('\n', problems).ShouldContain("missing from");
    }

    [Test]
    public void CanonicalRazorPath_WhenCodeBehind_StripsCsSuffix()
    {
        TestToPageMap.CanonicalRazorPath(@"src\UI.Shared\Pages\Login.razor.cs")
            .ShouldBe("src/UI.Shared/Pages/Login.razor");
    }

    [Test]
    public void GetTestsForChangedRazorScript_WhenRead_LoadsTheCommittedMap()
    {
        var script = RepositoryCheckout.Current.ReadText("scripts/acceptance/Get-TestsForChangedRazor.ps1");

        script.ShouldContain("test-to-page-map.json");
        script.ShouldContain("Get-CanonicalRazorPath");
        script.ShouldContain("FullyQualifiedName~");
    }

    private static TestToPageMap SampleMap()
    {
        return new TestToPageMap
        {
            Tests =
            [
                new TestToPageMapEntry
                {
                    Class = "ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests",
                    Pages = ["src/UI.Shared/Pages/Login.razor", "src/UI.Shared/Pages/Index.razor"],
                },
                new TestToPageMapEntry
                {
                    Class = "ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests",
                    Pages = ["src/UI.Shared/Pages/Index.razor"],
                },
            ],
        };
    }
}
