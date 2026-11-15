using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchSelectionPagingBulkExportTests
{
    [Test]
    public async Task ShouldShowBulkBar_WhenRowSelected()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(0);

        await component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.RowCheckbox, "WO-001"))
            .ChangeAsync(new() { Value = true });

        var bar = component.Find("[data-testid='BulkActionBar']");
        bar.GetAttribute("role").ShouldBe("toolbar");
        bar.QuerySelector("[data-testid='BulkSelectedCount']")!.TextContent.ShouldBe("1 selected");
        component.Find("tbody tr").ClassList.ShouldContain("row-selected");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).TextContent
            .ShouldEndWith("1 selected");
    }

    [Test]
    public async Task SelectAll_SelectsCurrentPageOnly()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderBuilder.ManyDrafts(30)).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.SelectAllCheckbox, true);

        component.Find("[data-testid='BulkSelectedCount']").TextContent.ShouldBe("25 selected");
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.SelectAllCheckbox)
            .HasAttribute("checked").ShouldBeTrue();
    }

    [Test]
    public async Task ShouldShowSecondPage_WhenNextClicked()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithOrders(WorkOrderBuilder.ManyDrafts(30)).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearchPage>();
        component.FindAll("tbody tr").Count.ShouldBe(25);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PagePrevButton))
            .HasAttribute("disabled").ShouldBeTrue();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PageButton, "1"))
            .GetAttribute("aria-current").ShouldBe("page");

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.PageNextButton);

        AssertSecondPage(component, navigation);
    }

    private static void AssertSecondPage(IRenderedComponent<WorkOrderSearchPage> component, NavigationManager navigation)
    {
        component.FindAll("tbody tr").Count.ShouldBe(5);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsCountPill)).TextContent
            .ShouldBe("5 of 30");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ResultsRangeText)).TextContent
            .ShouldBe("Showing 26–30 of 30 work orders · 0 selected");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.PageNextButton))
            .HasAttribute("disabled").ShouldBeTrue();
        navigation.Uri.ShouldEndWith("/workorder/search?Page=2");
    }

    [Test]
    public async Task Export_DownloadsCsvOfFilteredRows()
    {
        var (matching, other) = WorkOrderSearchSteps.ExportFilterPair();
        var bus = new StubStatusFilteredBus(matching, other);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var module = WorkOrderSearchSteps.SetupSearchExportModule(ctx);
        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ExportButton)).TextContent
            .ShouldContain("Export CSV");
        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.StatusSelect,
            WorkOrderStatus.Assigned.Key);
        bus.LastSpecification!.StatusKey.ShouldBe(WorkOrderStatus.Assigned.Key);
        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.ExportButton);

        AssertExportCsv(module);
    }

    private static void AssertExportCsv(BunitJSModuleInterop module)
    {
        var invocation = module.VerifyInvoke("downloadCsv");
        invocation.Arguments[0]!.ToString()!.ShouldStartWith("work-orders-");
        var csv = invocation.Arguments[1]!.ToString()!;
        csv.ShouldContain("WO-KEEP");
        csv.ShouldNotContain("WO-DROP");
        csv.ShouldContain("\"Say \"\"hi\"\", now\"");
    }

    [Test]
    public async Task BulkAssign_SendsAssignForSelectedRowsAndReportsOutcome()
    {
        var creator = WorkOrderSearchSteps.CreatorWhoCanCreate();
        creator.Id = Guid.NewGuid();
        var draft = WorkOrderSearchSteps.DraftOwnedBy(creator);
        var bus = new StubBulkBus([draft]);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).WithCurrentUser(creator).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.RowCheckbox, "WO-D1"))
            .ChangeAsync(new() { Value = true });
        await component.Find("[data-testid='BulkAssignSelect']").ChangeAsync(new() { Value = "hsimpson" });
        await component.Find("[data-testid='BulkAssignButton']").ClickAsync(new());

        bus.Commands.Single().TransitionVerbPresentTense.ShouldBe("Assign");
        bus.Commands.Single().WorkOrder.Assignee!.UserName.ShouldBe("hsimpson");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.BulkOutcome)).TextContent
            .ShouldBe("1 updated, 0 skipped");
        component.FindAll("[data-testid='BulkActionBar']").Count.ShouldBe(0);
    }
}
