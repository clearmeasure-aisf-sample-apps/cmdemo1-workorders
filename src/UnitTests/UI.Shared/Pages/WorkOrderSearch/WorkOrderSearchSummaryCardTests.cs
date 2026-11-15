using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchSummaryCardTests
{
    [Test]
    public async Task ShouldRenderSummaryCardsFromSummaryQuery()
    {
        var bus = new StubBus { Summary = new WorkOrderSearchSummary { OpenCount = 24, OverdueCount = 3, DueThisWeekCount = 5 } };
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find("[data-testid='SummaryCardOpen'] .summary-card-value").TextContent.ShouldBe("24");
        component.Find("[data-testid='SummaryCardOverdue'] .summary-card-value").TextContent.ShouldBe("3");
        component.Find("[data-testid='SummaryCardDueThisWeek'] .summary-card-value").TextContent.ShouldBe("5");
    }

    [Test]
    public async Task ClickingOverdueCard_AppliesOnlyOverdueFilter()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Creator=tlovejoy&Priority=Low");
        var component = ctx.Render<WorkOrderSearchPage>();

        await component.Find("[data-testid='SummaryCardOverdue']").ClickAsync(new());

        bus.LastSpecification!.OverdueOnly.ShouldBeTrue();
        bus.LastSpecification.Creator.ShouldBeNull();
        bus.LastSpecification.Priority.ShouldBeNull();
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.OverdueOnlyToggle)
            .HasAttribute("checked").ShouldBeTrue();
        navigation.Uri.ShouldEndWith("/workorder/search?OverdueOnly=true");
    }

    [Test]
    public async Task ClickingDueThisWeekCard_AppliesOnlyDueThisWeekFilter()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Creator=tlovejoy&Priority=Low");
        var component = ctx.Render<WorkOrderSearchPage>();

        await component.Find("[data-testid='SummaryCardDueThisWeek']").ClickAsync(new());

        bus.LastSpecification!.DueThisWeekOnly.ShouldBeTrue();
        bus.LastSpecification.Creator.ShouldBeNull();
        bus.LastSpecification.Priority.ShouldBeNull();
        navigation.Uri.ShouldEndWith("/workorder/search?DueThisWeekOnly=true");
    }
}
