using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchHeaderTests
{
    [Test]
    public async Task ShouldStillRenderResultsAndNewButton_WhenLoadingSavedViewsFails()
    {
        var creator = WorkOrderSearchSteps.CreatorWhoCanCreate();
        var bus = new StubBus { FailSavedViewsQuery = true };
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).WithCurrentUser(creator).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.NewWorkOrderButton)).Count.ShouldBe(1);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent.ShouldBe("2 of 2");
    }

    [Test]
    public async Task ShouldRenderSearchHintWithExactText()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var hint = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SearchHint));
        hint.TextContent.ShouldBe("Find, triage and track maintenance requests across all church facilities.");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnSearchHeading()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var heading = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SearchHeadingTooltip));
        heading.GetAttribute("title").ShouldBe("Search every work order");
    }

    [Test]
    public async Task ShouldRenderSharedPageHeader()
    {
        var creator = WorkOrderSearchSteps.CreatorWhoCanCreate();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithCurrentUser(creator).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var header = component.FindComponent<PageHeader>();
        header.Find($"h1{WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SearchHeadingTooltip)}")
            .TextContent.ShouldBe("Work Order Search");
        header.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SearchHint)).ClassList.ShouldContain("page-subtitle");
        header.FindAll($".page-actions {WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ExportButton)}").Count.ShouldBe(1);
        header.FindAll($".page-actions {WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.NewWorkOrderButton)}").Count.ShouldBe(1);
    }

    [Test]
    public async Task ResultsHeader_ZeroResults_ShowsPluralWorkOrders()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(new StubBus([])).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent.ShouldBe("0 of 0");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).TextContent
            .ShouldBe("Showing 0 of 0 work orders · 0 selected");
    }

    [Test]
    public async Task ResultsHeader_OneResult_ShowsSingularWorkOrder()
    {
        var row = WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("Only one").WithStatus(WorkOrderStatus.Draft).Build();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(row).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent.ShouldBe("1 of 1");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).TextContent
            .ShouldBe("Showing 1–1 of 1 work order · 0 selected");
    }

    [Test]
    public async Task ResultsHeader_TwoResults_ShowsPluralWorkOrders()
    {
        var rows = new[]
        {
            WorkOrderBuilder.Create().WithNumber("WO-001").WithTitle("First").WithStatus(WorkOrderStatus.Draft).Build(),
            WorkOrderBuilder.Create().WithNumber("WO-002").WithTitle("Second").WithStatus(WorkOrderStatus.Assigned).Build()
        };
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithOrders(rows).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent.ShouldBe("2 of 2");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).TextContent
            .ShouldBe("Showing 1–2 of 2 work orders · 0 selected");
    }

    [Test]
    public async Task ShouldRenderNewWorkOrderButton_WhenUserCanCreate()
    {
        var creator = WorkOrderSearchSteps.CreatorWhoCanCreate();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithCurrentUser(creator).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.NewWorkOrderButton)).GetAttribute("href")
            .ShouldBe("workorder/manage?mode=New");
    }

    [Test]
    public async Task ShouldNotRenderNewWorkOrderButton_WhenUserCannotCreate()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.NewWorkOrderButton)).Count.ShouldBe(0);
    }
}
