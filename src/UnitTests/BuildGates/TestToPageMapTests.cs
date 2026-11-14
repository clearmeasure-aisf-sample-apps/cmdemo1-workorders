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
    public void TestsForRazorPaths_WhenAComponentChanges_ExpandsToHostPageFixtures()
    {
        var map = SampleMap();

        var selected = map.TestsForRazorPaths(["src/UI.Shared/Components/Teams/TeamCards.razor"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors.TeamsPageTests");
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
    public void GetTestsForChangedRazorScript_WhenRead_AcceptsPipelineAndRejectsPathsFileDash()
    {
        var script = RepositoryCheckout.Current.ReadText("scripts/acceptance/Get-TestsForChangedRazor.ps1");

        script.ShouldContain("test-to-page-map.json");
        script.ShouldContain("ValueFromPipeline");
        script.ShouldContain("Console]::IsInputRedirected");
        script.ShouldContain("PathsFile '-' is not supported");
        script.ShouldContain("components");
        script.ShouldContain("AllowEmpty");
    }

    [Test]
    public void GetTestsForChangedRazorScript_WhenTeamCardsPipedToFile_SelectsTeamsFixtures()
    {
        var root = RepositoryCheckout.Current.Root;
        var script = Path.Combine(root, "scripts/acceptance/Get-TestsForChangedRazor.ps1");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "pwsh",
            ArgumentList = { "-NoProfile", "-File", script },
            WorkingDirectory = root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(psi)!;
        process.StandardInput.WriteLine("src/UI.Shared/Components/Teams/TeamCards.razor");
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(60_000).ShouldBeTrue();
        process.ExitCode.ShouldBe(0);
        output.ShouldContain("TeamsPageTests");
    }

    [Test]
    public void GetTestsForChangedRazorScript_WhenLoginPipedToFile_SelectsLoginFixtures()
    {
        var root = RepositoryCheckout.Current.Root;
        var script = Path.Combine(root, "scripts/acceptance/Get-TestsForChangedRazor.ps1");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "pwsh",
            ArgumentList = { "-NoProfile", "-File", script },
            WorkingDirectory = root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(psi)!;
        process.StandardInput.WriteLine("src/UI.Shared/Pages/Login.razor");
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(60_000).ShouldBeTrue();
        process.ExitCode.ShouldBe(0);
        output.ShouldContain("LoginTests");
    }

    private static TestToPageMap SampleMap()
    {
        return new TestToPageMap
        {
            Tests = [LoginEntry(), LandingEntry(), TeamsEntry()],
            Components = [TeamCardsComponent()],
        };
    }

    private static TestToPageMapEntry LoginEntry() =>
        MapEntry("Authentication.LoginTests", "Pages/Login.razor", "Pages/Index.razor");

    private static TestToPageMapEntry LandingEntry() =>
        MapEntry("App.LandingPageTests", "Pages/Index.razor");

    private static TestToPageMapEntry TeamsEntry() =>
        MapEntry("TeamsAndVendors.TeamsPageTests", "Pages/TeamsAndVendors.razor");

    private static TestToPageMapEntry MapEntry(string relativeClass, params string[] sharedPaths) =>
        new()
        {
            Class = $"ClearMeasure.Bootcamp.AcceptanceTests.{relativeClass}",
            Pages = sharedPaths.Select(static path => $"src/UI.Shared/{path}").ToList(),
        };

    private static TestToPageComponentEntry TeamCardsComponent() =>
        new()
        {
            Path = "src/UI.Shared/Components/Teams/TeamCards.razor",
            Pages = ["src/UI.Shared/Pages/TeamsAndVendors.razor"],
        };
}
