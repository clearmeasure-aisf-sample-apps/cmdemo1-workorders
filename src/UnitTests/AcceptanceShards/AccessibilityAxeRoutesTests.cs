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
        var routes = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/App/AccessibilityAxeRoute.cs");

        tests.ShouldContain("[Parallelizable(ParallelScope.Children)]");
        tests.ShouldContain("Should_HaveNoSeriousOrCriticalAxeViolations_OnRoute");
        tests.ShouldContain("[TestCaseSource(nameof(RouteKeys))]");
        tests.ShouldNotContain("OnEveryRouteInBothThemes");
        System.Text.RegularExpressions.Regex.Matches(tests, @"yield return ").Count.ShouldBeGreaterThanOrEqualTo(12);
        routes.ShouldContain("AccessibilityAxeRoutes");
    }

    [Test]
    public void PhoneWidthTests_WhenRead_SplitOneCasePerRoute()
    {
        var tests = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/App/PhoneWidthNoDocumentScrollTests.cs");

        tests.ShouldContain("[Parallelizable(ParallelScope.Children)]");
        tests.ShouldContain("ShouldMatchDocumentWidthToViewport_At390_OnRoute");
        tests.ShouldContain("[TestCaseSource(nameof(RouteKeys))]");
        tests.ShouldNotContain("OnEveryRoute()");
        System.Text.RegularExpressions.Regex.Matches(tests, @"yield return ").Count.ShouldBeGreaterThanOrEqualTo(12);
    }

    [Test]
    public void AxeCatalog_WhenRead_CountsYieldedRouteKeys()
    {
        var axe = AcceptanceTestCatalog.Read(RepositoryCheckout.Current)
            .Single(item => item.Name.EndsWith("AccessibilityAxeTests", StringComparison.Ordinal));
        axe.TestCount.ShouldBe(14);
    }

    [Test]
    public void PhoneWidthCatalog_WhenRead_CountsYieldedRouteKeys()
    {
        var width = AcceptanceTestCatalog.Read(RepositoryCheckout.Current)
            .Single(item => item.Name.EndsWith("PhoneWidthNoDocumentScrollTests", StringComparison.Ordinal));
        width.TestCount.ShouldBe(19);
    }
}
