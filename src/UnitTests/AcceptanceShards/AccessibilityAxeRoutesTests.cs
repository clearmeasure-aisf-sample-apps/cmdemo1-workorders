using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

[TestFixture]
public class AccessibilityAxeRoutesTests
{
    [Test]
    public void AxeTests_WhenRead_SplitOneCasePerRoute()
    {
        var tests = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/App/AccessibilityAxeTests.cs");
        var routes = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/App/AccessibilityAxeRoutes.cs");

        tests.ShouldContain("[Parallelizable(ParallelScope.All)]");
        tests.ShouldContain("Should_HaveNoSeriousOrCriticalAxeViolations_OnRoute");
        tests.ShouldContain("[TestCaseSource(nameof(RouteKeys))]");
        tests.ShouldNotContain("OnEveryRouteInBothThemes");
        tests.ShouldNotContain("AcceptanceTeams");
        System.Text.RegularExpressions.Regex.Matches(tests, "yield return ").Count.ShouldBe(13);
        routes.ShouldContain("AccessibilityAxeRoutes");
    }

    [Test]
    public void AxeTeamsFixture_WhenRead_IsSerialAfterParallelWithoutGlobalWipe()
    {
        var tests = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/App/AccessibilityAxeTeamsTests.cs");
        var serial = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/acceptance-serial-lane.json");

        tests.ShouldContain("NonParallelizable");
        tests.ShouldContain("Category(\"SerialLane\")");
        tests.ShouldContain("Should_HaveNoSeriousOrCriticalAxeViolations_OnTeamsRoute");
        tests.ShouldNotContain("AcceptanceTeams");
        tests.ShouldNotContain("ExecuteDelete");
        serial.ShouldContain("AccessibilityAxeTeamsTests");
    }

    [Test]
    public void PhoneWidthTests_WhenRead_SplitOneCasePerRoute()
    {
        var tests = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/App/PhoneWidthNoDocumentScrollTests.cs");

        tests.ShouldContain("[Parallelizable(ParallelScope.All)]");
        tests.ShouldContain("ShouldMatchDocumentWidthToViewport_At390_OnRoute");
        tests.ShouldContain("[TestCaseSource(nameof(RouteKeys))]");
        tests.ShouldNotContain("OnEveryRoute()");
        System.Text.RegularExpressions.Regex.Matches(tests, "yield return ").Count.ShouldBeGreaterThanOrEqualTo(12);
    }

    [Test]
    public void AxeCatalog_WhenRead_CountsYieldedRouteKeys()
    {
        var axe = AcceptanceTestCatalog.Read(RepositoryCheckout.Current)
            .Single(item => item.Name.EndsWith("AccessibilityAxeTests", StringComparison.Ordinal)
                && !item.Name.EndsWith("AccessibilityAxeTeamsTests", StringComparison.Ordinal));
        axe.TestCount.ShouldBe(13);
    }

    [Test]
    public void AxeTeamsCatalog_WhenRead_CountsOneSerialCase()
    {
        var teams = AcceptanceTestCatalog.Read(RepositoryCheckout.Current)
            .Single(item => item.Name.EndsWith("AccessibilityAxeTeamsTests", StringComparison.Ordinal));
        teams.TestCount.ShouldBe(1);
    }

    [Test]
    public void PhoneWidthCatalog_WhenRead_CountsYieldedRouteKeys()
    {
        var width = AcceptanceTestCatalog.Read(RepositoryCheckout.Current)
            .Single(item => item.Name.EndsWith("PhoneWidthNoDocumentScrollTests", StringComparison.Ordinal));
        width.TestCount.ShouldBe(19);
    }
}
