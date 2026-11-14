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
        var selected = TestToPageMapMother.LoginLandingAndTeams()
            .TestsForRazorPaths(["src/UI.Shared/Pages/Login.razor"]);

        selected.ShouldBe(
        [
            "ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests",
        ]);
    }

    [Test]
    public void TestsForRazorPaths_WhenCodeBehindChanges_MatchesTheRazorPage()
    {
        var selected = TestToPageMapMother.LoginLandingAndTeams()
            .TestsForRazorPaths(["src/UI.Shared/Pages/Login.razor.cs"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests");
    }

    [Test]
    public void TestsForRazorPaths_WhenAComponentChanges_ExpandsToHostPageFixtures()
    {
        var selected = TestToPageMapMother.LoginLandingAndTeams()
            .TestsForRazorPaths(["src/UI.Shared/Components/Teams/TeamCards.razor"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors.TeamsPageTests");
    }

    [Test]
    public void TestsForRazorPaths_WhenPageHeaderChanges_SelectsPagesThatRenderIt()
    {
        var map = TestToPageMap.Load(RepositoryCheckout.Current);

        var selected = map.TestsForRazorPaths(["src/UI.Shared/Components/PageHeader.razor"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.App.ReportsPageTests");
        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests");
        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests");
    }

    [Test]
    public void TestsForRazorPaths_WhenLastWorkOrderChanges_SelectsNavMenuFixtures()
    {
        var map = TestToPageMap.Load(RepositoryCheckout.Current);

        var selected = map.TestsForRazorPaths(["src/UI.Shared/Components/LastWorkOrder.razor"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.App.NavMenuTests");
    }

    [Test]
    public void TestsForRazorPaths_WhenAssigneeAvatarChanges_RecursesThroughSearchChildren()
    {
        var map = TestToPageMap.Load(RepositoryCheckout.Current);

        var selected = map.TestsForRazorPaths(["src/UI.Shared/Components/Search/AssigneeAvatar.razor"]);

        selected.ShouldContain("ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests");
    }

    [Test]
    public async Task Script_WhenSelfTestRuns_ExitsZero()
    {
        var checkout = RepositoryCheckout.Current;
        var script = checkout.PathOf("scripts/acceptance/Get-TestsForChangedRazor.ps1");

        var result = await ExternalCommand.Pwsh(script, "-SelfTest").In(checkout.Root).RunAsync();

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Get-TestsForChangedRazor.Tests.ps1 passed");
    }

    [Test]
    public void TestsForRazorPaths_WhenUnmappedRazorChanges_ReturnsEmpty()
    {
        TestToPageMapMother.LoginLandingAndTeams()
            .TestsForRazorPaths(["src/UI.Shared/Pages/Reports.razor"])
            .ShouldBeEmpty();
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
        script.ShouldContain("SelfTest");
        script.ShouldContain("Get-TestsForChangedRazor.Tests.ps1");
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

}
