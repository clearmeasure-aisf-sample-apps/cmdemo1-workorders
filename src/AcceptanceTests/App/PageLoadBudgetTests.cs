using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.Acceptance;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// API-call and wall-clock budgets for Dashboard, Search, and Teams &amp; Vendors.
/// Each measurement is an in-app navigation after the shell has gone quiet, so the
/// Blazor download is not part of the number. The helper counts same-origin <c>/api/</c>
/// requests. Reintroducing the per-employee <c>EmployeeWorkLoadQuery</c> on Teams
/// &amp; Vendors (the N+1 removed in #1491) exceeds <see cref="TeamsMaxApiCalls"/>.
/// </summary>
[TestFixture]
public class PageLoadBudgetTests : AcceptanceTestBase
{
    /// <summary>
    /// Fulfillers inserted before the Teams measurement. The fixed page sends one
    /// workload summary no matter how many fulfillers exist. The removed loop sent
    /// one <c>EmployeeWorkLoadQuery</c> per fulfiller, so these rows plus the
    /// signed-in user push that loop past <see cref="TeamsMaxApiCalls"/> even when
    /// the database has no other assignable employees.
    /// </summary>
    private const int ExtraFulfillers = 8;

    /// <summary>
    /// Teams issues four bus calls on a signed-in navigation: workload summary,
    /// the shell notification refresh, teams, and vendors (vendor management is on
    /// in the acceptance host). Five leaves one call of room and fails the
    /// per-employee loop.
    /// </summary>
    private const int TeamsMaxApiCalls = 5;

    private const int TeamsMinApiCalls = 3;

    /// <summary>
    /// Dashboard issued five calls locally: the signed-in user, the notification
    /// refresh, the search summary, status counts, and recent work orders.
    /// Six leaves one call of room.
    /// </summary>
    private const int DashboardMaxApiCalls = 6;

    private const int DashboardMinApiCalls = 4;

    /// <summary>
    /// Search issued seven calls locally: employees, the notification refresh,
    /// buildings, saved views, the signed-in user, the search summary, and the
    /// specification query. Eight leaves one call of room.
    /// </summary>
    private const int SearchMaxApiCalls = 8;

    private const int SearchMinApiCalls = 6;

    /// <summary>
    /// Local signed-in navigations finished in 67 ms (Dashboard), 142 ms (Teams),
    /// and 192 ms (Search) against SQLite on an idle machine, all well under 2 seconds.
    /// The acceptance ceiling is 8 seconds: about forty times the slowest sample, so a
    /// parallel CI shard sharing SQL Server still passes. The API-call ceilings are the
    /// deterministic gate for the Teams N+1 regression; this time ceiling is the
    /// backstop for a load that stalls.
    /// </summary>
    private static readonly TimeSpan LoadCeiling = TimeSpan.FromSeconds(8);

    [Test]
    public async Task ShouldStayWithinBudget_WhenTeamsAndVendorsLoads()
    {
        SeedFulfillers(ExtraFulfillers);
        await LoginAsCurrentUser();
        await PageLoadBudget.WaitForQuietAsync(Page, ServerFixture.ApplicationBaseUrl);

        var measurement = await MeasureClickAsync(
            nameof(NavMenu.Elements.TeamsAndVendors),
            Page.Locator(".teams-table"));

        AssertWithin(measurement, TeamsMinApiCalls, TeamsMaxApiCalls);
    }

    [Test]
    public async Task ShouldStayWithinBudget_WhenDashboardLoads()
    {
        await LoginAsCurrentUser();
        await OpenSearchAndSettleAsync();

        var measurement = await MeasureClickAsync(
            nameof(NavMenu.Elements.Dashboard),
            Page.GetByTestId(nameof(IndexPage.Elements.StatusDashboard)));

        AssertWithin(measurement, DashboardMinApiCalls, DashboardMaxApiCalls);
    }

    [Test]
    public async Task ShouldStayWithinBudget_WhenSearchLoads()
    {
        await LoginAsCurrentUser();
        await PageLoadBudget.WaitForQuietAsync(Page, ServerFixture.ApplicationBaseUrl);

        var measurement = await MeasureClickAsync(
            nameof(NavMenu.Elements.Search),
            Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill)));

        AssertWithin(measurement, SearchMinApiCalls, SearchMaxApiCalls);
    }

    private async Task OpenSearchAndSettleAsync()
    {
        await PageLoadBudget.WaitForQuietAsync(Page, ServerFixture.ApplicationBaseUrl);
        var search = Page.GetByTestId(nameof(NavMenu.Elements.Search));
        await Expect(search).ToBeVisibleAsync();
        await search.EvaluateAsync("el => el.click()");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill))).ToBeVisibleAsync();
        await PageLoadBudget.WaitForQuietAsync(Page, ServerFixture.ApplicationBaseUrl);
    }

    private async Task<PageLoadMeasurement> MeasureClickAsync(string linkTestId, ILocator ready)
    {
        var link = Page.GetByTestId(linkTestId);
        await Expect(link).ToBeVisibleAsync();
        var measurement = await PageLoadBudget.MeasureAsync(
            Page,
            ServerFixture.ApplicationBaseUrl,
            async () => { await link.EvaluateAsync("el => el.click()"); },
            ready);
        // ReSharper disable once MethodHasAsyncOverload -- TestContext output is synchronous diagnostic text
        TestContext.Out.WriteLine(measurement.Describe());
        return measurement;
    }

    private static void AssertWithin(PageLoadMeasurement measurement, int minimumCalls, int maximumCalls)
    {
        var detail = measurement.Describe();
        var verdict = PageLoadBudgetRules.Evaluate(
            measurement.Requests,
            ServerFixture.ApplicationBaseUrl,
            minimumCalls,
            maximumCalls);
        verdict.Passed.ShouldBeTrue(verdict.Detail + " " + detail);
        verdict.ApiCallCount.ShouldBe(measurement.ApiCallCount, detail);
        measurement.Elapsed.ShouldBeLessThan(LoadCeiling, detail);
    }

    private void SeedFulfillers(int count)
    {
        using var context = TestHost.NewDbContext();
        for (var index = 0; index < count; index++)
        {
            var userName = $"fulfiller_{TestTag}_{index}";
            var employee = new Employee(userName, "Fulfill", index.ToString(), $"{userName}@example.com");
            employee.AddRole(new Role($"worker_{TestTag}_{index}", false, true));
            context.Add(employee);
        }

        context.SaveChanges();
    }
}
