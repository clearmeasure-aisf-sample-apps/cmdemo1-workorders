using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components.Teams;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

[TestFixture]
public class TeamWorkLoadTableTests
{
    private static readonly EmployeeWorkLoadSummary[] TwoEmployees =
    [
        TeamsMother.Loaded("zed", "Zimmer", assignedAndOpen: 1),
        TeamsMother.Loaded("amy", "Adams", assignedAndOpen: 5)
    ];

    [Test]
    public async Task ShouldListEmployeesByLastName_WhenRendered()
    {
        await using var ctx = new BunitContext();

        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, TwoEmployees));

        RowNames(table).ShouldBe([$"{TeamsAndVendors.Elements.TableRowName}amy", $"{TeamsAndVendors.Elements.TableRowName}zed"]);
    }

    [Test]
    public async Task ShouldListTheBusiestFirst_WhenAssignedOpenIsClicked()
    {
        await using var ctx = new BunitContext();
        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, TwoEmployees));

        await table.Find(".col-assigned-open .sort-header").ClickAsync(new());

        RowNames(table).ShouldBe([$"{TeamsAndVendors.Elements.TableRowName}amy", $"{TeamsAndVendors.Elements.TableRowName}zed"]);
        table.Find(".col-assigned-open .sort-indicator").TextContent.Trim().ShouldBe("▼");
    }

    [Test]
    public async Task ShouldSayThereAreNoAssignableEmployees_WhenEmpty()
    {
        await using var ctx = new BunitContext();

        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, []));

        table.Markup.ShouldContain("No assignable employees found.");
    }

    [Test]
    public void ShouldSearchTheEmployeesOpenWork_WhenTheSearchLinkIsBuilt()
    {
        TeamWorkLoadTable.OpenWorkSearch("ada lovelace").ShouldBe("workorder/search?Assignee=ada%20lovelace&OpenOnly=true");
    }

    [Test]
    public async Task ShouldExposeAscendingNameSort_WhenFirstRendered()
    {
        await using var ctx = new BunitContext();
        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, TwoEmployees));

        table.Find("th.col-name").GetAttribute("aria-sort").ShouldBe("ascending");
        table.Find("th.col-assigned-open").GetAttribute("aria-sort").ShouldBe("none");
    }

    [Test]
    public async Task ShouldFlipAssignedOpenAriaSort_WhenItsHeaderIsClickedTwice()
    {
        await using var ctx = new BunitContext();
        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, TwoEmployees));

        await table.Find(".col-assigned-open .sort-header").ClickAsync(new());

        table.Find("th.col-assigned-open").GetAttribute("aria-sort").ShouldBe("descending");
        table.Find("th.col-name").GetAttribute("aria-sort").ShouldBe("none");

        await table.Find(".col-assigned-open .sort-header").ClickAsync(new());

        table.Find("th.col-assigned-open").GetAttribute("aria-sort").ShouldBe("ascending");
        table.Find("th.col-name").GetAttribute("aria-sort").ShouldBe("none");
    }

    [Test]
    public async Task ShouldOmitAriaSort_WhenHeaderIsNotSortable()
    {
        await using var ctx = new BunitContext();
        var table = ctx.Render<TeamWorkLoadTable>(parameters => parameters.Add(p => p.Summaries, TwoEmployees));

        table.Find("th.col-role").HasAttribute("aria-sort").ShouldBeFalse();
        table.Find("th.col-in-progress").HasAttribute("aria-sort").ShouldBeFalse();
        table.Find("th.col-overdue").HasAttribute("aria-sort").ShouldBeFalse();
        table.Find("th.col-completed").HasAttribute("aria-sort").ShouldBeFalse();
    }

    private static string?[] RowNames(IRenderedComponent<TeamWorkLoadTable> table) =>
        table.FindAll(".teams-table-row .col-name").Select(cell => cell.GetAttribute("data-testid")).ToArray();
}
