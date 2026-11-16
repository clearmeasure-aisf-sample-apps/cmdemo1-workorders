using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchAssignedToMeTests
{
    [Test]
    public async Task AssignedToMeCheckbox_WhenSessionStateIsTrue_RestoresCheckboxAndFiltersOnLoad()
    {
        var state = new WorkOrderSearchState { AssignedToMe = true };
        var stubBus = new StubBusWithAssigneeCapture();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(stubBus).WithSearchState(state).Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var checkbox = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssignedToMeCheckbox);
        checkbox.HasAttribute("checked").ShouldBeTrue();
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssigneeSelect).HasAttribute("disabled")
            .ShouldBeTrue();
        stubBus.LastAssigneeQueried.ShouldBe(WorkOrderSearchPageBuilder.DefaultUserName);
    }

    [Test]
    public async Task AssignedToMeCheckbox_ShouldBeRendered()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();

        var component = ctx.Render<WorkOrderSearchPage>();

        var checkbox = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssignedToMeCheckbox);
        checkbox.ShouldNotBeNull();
        checkbox.HasAttribute("checked").ShouldBeFalse();
    }

    [Test]
    public async Task AssignedToMeCheckbox_WhenChecked_SetsAssigneeToCurrentUser()
    {
        var stubBus = new StubBusWithAssigneeCapture();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(stubBus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.AssignedToMeCheckbox, true);

        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssigneeSelect).HasAttribute("disabled")
            .ShouldBeTrue();
        stubBus.LastAssigneeQueried.ShouldBe(WorkOrderSearchPageBuilder.DefaultUserName);
    }

    [Test]
    public async Task AssignedToMeCheckbox_WhenUnchecked_ClearsAssigneeFilter()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var checkbox = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssignedToMeCheckbox);

        await checkbox.ChangeAsync(new() { Value = true });
        await checkbox.ChangeAsync(new() { Value = false });

        var assigneeSelect = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssigneeSelect);
        assigneeSelect.HasAttribute("disabled").ShouldBeFalse();
        assigneeSelect.GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Test]
    public async Task ClearFiltersButton_WhenClicked_ResetsAssignedToMeCheckbox()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.AssignedToMeCheckbox, true);
        var clearButton = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.ClearFiltersButton);
        await clearButton.ClickAsync(new());

        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssignedToMeCheckbox)
            .HasAttribute("checked").ShouldBeFalse();
        clearButton.HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task SessionAssignedToMe_OnBareUrl_DoesNotRewriteUrl()
    {
        var bus = new StubBusWithAssigneeCapture();
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithBus(bus)
            .WithSearchState(new WorkOrderSearchState { AssignedToMe = true })
            .Build();
        var navigation = (BunitNavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search");
        var historyBefore = navigation.History.Count;

        ctx.Render<WorkOrderSearchPage>();

        bus.LastAssigneeQueried.ShouldBe(WorkOrderSearchPageBuilder.DefaultUserName);
        navigation.Uri.ShouldEndWith("/workorder/search");
        navigation.History.Count.ShouldBe(historyBefore);
    }
}
