using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchFilterAndUrlTests
{
    [Test]
    public async Task ShouldLoadDropDownsInitiallyOnLoad()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        var creatorSelect = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.CreatorSelect);
        var assigneeSelect = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssigneeSelect);
        var statusSelect = WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.StatusSelect);

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();
        AssertDropDownOptionCounts(creatorSelect, assigneeSelect, statusSelect);
    }

    private static void AssertDropDownOptionCounts(
        AngleSharp.Dom.IElement creatorSelect,
        AngleSharp.Dom.IElement assigneeSelect,
        AngleSharp.Dom.IElement statusSelect)
    {
        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(6);
        creatorOptions[0].TextContent.ShouldBe("Anyone");
        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(6);
        assigneeOptions[0].TextContent.ShouldBe("Anyone");
        var statusOptions = statusSelect.QuerySelectorAll("option");
        statusOptions.Length.ShouldBe(6);
        statusOptions[0].TextContent.ShouldBe("All statuses");
    }

    [Test]
    public async Task ShouldAssociateFilterLabelsWithMatchingSelectIds()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        AssertLabelForMatchesSelectId(component, WorkOrderSearchPage.Elements.CreatorSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearchPage.Elements.AssigneeSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearchPage.Elements.StatusSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearchPage.Elements.PrioritySelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearchPage.Elements.BuildingSelect);
        AssertLabelForMatchesSelectId(component, WorkOrderSearchPage.Elements.RoomInput);
    }

    private static void AssertLabelForMatchesSelectId(
        IRenderedComponent<WorkOrderSearchPage> component,
        WorkOrderSearchPage.Elements element)
    {
        var id = element.ToString();
        component.Find($"#{id}").ShouldNotBeNull();
        component.Find($"label[for='{id}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();
        workOrderTable.QuerySelectorAll("tbody tr").Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithCreatorFilterOnInitialLoad()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Creator", "somename"));

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(".grid-data").QuerySelectorAll("tbody tr").Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithAssigneeFilterOnInitialLoad()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Assignee", "somename"));

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(".grid-data").QuerySelectorAll("tbody tr").Length.ShouldBe(2);
    }

    [Test]
    public async Task ShouldLoadWorkOrderTableWithStatusFilterOnInitialLoad()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(
            navigationManager.GetUriWithQueryParameter("Status", WorkOrderStatus.Assigned.Key));

        var component = ctx.Render<WorkOrderSearchPage>();

        component.Find(".grid-data").QuerySelectorAll("tbody tr").Length.ShouldBe(2);
    }

    [Test]
    public async Task AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.SetCreatorAssigneeStatusFiltersAsync(component);

        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();
        workOrderTable.QuerySelectorAll("tbody tr").Length.ShouldBe(2);
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeDisabled_WhenAllFiltersAreEmpty()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.ClearFiltersButton)
            .HasAttribute("disabled").ShouldBeTrue();
    }

    [Test]
    public async Task ClearFiltersButton_ShouldBeEnabled_WhenAtLeastOneFilterIsSet()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.CreatorSelect, "jpalermo");

        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.ClearFiltersButton)
            .HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task ClickingClearFiltersButton_ShouldResetAllFiltersToEmpty_AndTriggerSearch()
    {
        var stubBus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(stubBus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.SetCreatorAssigneeStatusFiltersAsync(component);
        var sendCountBeforeClear = stubBus.SendCallCount;
        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.ClearFiltersButton}");

        AssertFiltersCleared(component, stubBus, sendCountBeforeClear);
    }

    private static void AssertFiltersCleared(
        IRenderedComponent<WorkOrderSearchPage> component,
        StubBus stubBus,
        int sendCountBeforeClear)
    {
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.CreatorSelect)
            .GetAttribute("value").ShouldBeNullOrEmpty();
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.AssigneeSelect)
            .GetAttribute("value").ShouldBeNullOrEmpty();
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.StatusSelect)
            .GetAttribute("value").ShouldBeNullOrEmpty();
        stubBus.SendCallCount.ShouldBeGreaterThan(sendCountBeforeClear);
    }

    [Test]
    public async Task ShouldReport_HasActiveFilters_True_WhenOverdueOnlyIsSet()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.OverdueOnlyToggle, true);

        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.ClearFiltersButton)
            .HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldReset_OverdueOnly_OnClearFilters()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        var instance = component.Instance;

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.OverdueOnlyToggle, true);
        instance.Model.Filters.OverdueOnly.ShouldBeTrue();
        await WorkOrderSearchSteps.ClickAsync(component, $"#{WorkOrderSearchPage.Elements.ClearFiltersButton}");

        instance.Model.Filters.OverdueOnly.ShouldBeFalse();
    }

    [Test]
    public void ShouldRemotableRequest_RoundTrip_OverdueOnly()
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchOverdueOnly(true);

        var json = System.Text.Json.JsonSerializer.Serialize(query);
        var rehydrated = System.Text.Json.JsonSerializer.Deserialize<WorkOrderSpecificationQuery>(json);

        rehydrated.ShouldNotBeNull();
        rehydrated.OverdueOnly.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldNotRenderSearchButton()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindAll("#SearchButton").Count.ShouldBe(0);
        component.FindAll("button[type='submit']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ChangingCreator_RunsSearchAndReplacesUrl()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearchPage>();
        var sendsBefore = bus.SendCallCount;

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.CreatorSelect, "tlovejoy");

        bus.SendCallCount.ShouldBeGreaterThan(sendsBefore);
        bus.LastSpecification!.Creator.ShouldNotBeNull();
        navigation.Uri.ShouldEndWith("/workorder/search?Creator=tlovejoy");
        ((BunitNavigationManager)navigation).History.First().Options.ReplaceHistoryEntry.ShouldBeTrue();
    }

    [Test]
    public async Task ChangingRoom_RunsSearchAndReplacesUrl()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Creator=tlovejoy");
        var component = ctx.Render<WorkOrderSearchPage>();
        var sendsBefore = bus.SendCallCount;

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.RoomInput, "101");

        bus.SendCallCount.ShouldBeGreaterThan(sendsBefore);
        bus.LastSpecification!.Room.ShouldBe("101");
        bus.LastSpecification.Creator.ShouldNotBeNull();
        navigation.Uri.ShouldEndWith("/workorder/search?Creator=tlovejoy&Room=101");
        ((BunitNavigationManager)navigation).History.First().Options.ReplaceHistoryEntry.ShouldBeTrue();
    }

    [Test]
    public async Task QueryString_Room_ShowsValueAndFilters()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?Room=101");

        var component = ctx.Render<WorkOrderSearchPage>();

        bus.LastSpecification!.Room.ShouldBe("101");
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.RoomInput)
            .GetAttribute("value").ShouldBe("101");
    }

    [Test]
    public async Task QueryString_PriorityBuildingAndQ_AppliedOnLoad()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        WorkOrderSearchSteps.NavigateTo(ctx,
            "workorder/search?Priority=High&Building=Kitchen&Q=organ&OpenOnly=true&CompletedWithinDays=30");

        var component = ctx.Render<WorkOrderSearchPage>();

        AssertPriorityBuildingQuery(bus, component);
    }

    private static void AssertPriorityBuildingQuery(StubBus bus, IRenderedComponent<WorkOrderSearchPage> component)
    {
        bus.LastSpecification!.Priority.ShouldBe(WorkOrderPriority.High);
        bus.LastSpecification.Building.ShouldBe("Kitchen");
        bus.LastSpecification.SearchText.ShouldBe("organ");
        bus.LastSpecification.OpenOnly.ShouldBeTrue();
        bus.LastSpecification.CompletedWithinDays.ShouldBe(30);
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.PrioritySelect)
            .GetAttribute("value").ShouldBe("High");
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.BuildingSelect)
            .GetAttribute("value").ShouldBe("Kitchen");
        WorkOrderSearchSteps.ChipKeys(component)
            .ShouldBe(["Priority", "Building", "OpenOnly", "CompletedWithinDays", "Q"]);
    }

    [Test]
    public async Task BuildingFilter_StaysAvailable_When_BuildingManagementIsOff()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create()
            .WithBus(bus)
            .WithFeatureFlags(new StubFeatureFlags(buildingManagement: false))
            .Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?Building=Kitchen");

        var component = ctx.Render<WorkOrderSearchPage>();

        bus.BuildingsQueryCount.ShouldBe(0);
        bus.LastSpecification!.Building.ShouldBe("Kitchen");
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.BuildingSelect)
            .GetAttribute("value").ShouldBe("Kitchen");
        component.Find($"label[for='{WorkOrderSearchPage.Elements.BuildingSelect}']").TextContent
            .ShouldBe("Building");
    }

    [Test]
    public async Task RemovingChip_ClearsThatFilter()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Creator=tlovejoy&OpenOnly=true");
        var component = ctx.Render<WorkOrderSearchPage>();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.ActiveFilterChip, "Creator"))
            .TextContent.ShouldContain("Creator: Timothy Lovejoy Jr");

        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.ActiveFilterChip, "OpenOnly");

        WorkOrderSearchSteps.ChipKeys(component).ShouldBe(["Creator"]);
        bus.LastSpecification!.OpenOnly.ShouldBeFalse();
        navigation.Uri.ShouldEndWith("/workorder/search?Creator=tlovejoy");
    }

    [Test]
    public async Task SelectWithValue_HasValueClass()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.StatusSelect).ClassList
            .ShouldNotContain("has-value");

        await WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.StatusSelect,
            WorkOrderStatus.Assigned.Key);

        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.StatusSelect).ClassList
            .ShouldContain("has-value");
        WorkOrderSearchSteps.FindById(component, WorkOrderSearchPage.Elements.CreatorSelect).ClassList
            .ShouldNotContain("has-value");
    }

    [Test]
    public async Task QueryString_DueThisWeekOnly_AppliedOnLoad()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        WorkOrderSearchSteps.NavigateTo(ctx, "workorder/search?DueThisWeekOnly=true");

        ctx.Render<WorkOrderSearchPage>();

        bus.LastSpecification!.DueThisWeekOnly.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldKeepLatestFilters_WhenEarlierSearchCompletesLast()
    {
        var bus = new StubHeldSearchBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var component = ctx.Render<WorkOrderSearchPage>();

        await StartTwoOverlappingSearches(component, bus);
        WorkOrderSearchSteps.CompleteHeldSearch(bus, 1, WorkOrderSearchSteps.NewestAssignedOrder());
        await component.WaitForAssertionAsync(() =>
            component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.WorkOrderLink, "NEWEST1"))
                .Count.ShouldBe(1));
        WorkOrderSearchSteps.CompleteHeldSearch(bus, 0, WorkOrderSearchSteps.StaleDraftOrder());

        await AssertNewestWins(component, navigation);
    }

    private static async Task StartTwoOverlappingSearches(
        IRenderedComponent<WorkOrderSearchPage> component,
        StubHeldSearchBus bus)
    {
        _ = WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.CreatorSelect, "tlovejoy");
        await component.WaitForAssertionAsync(() => bus.Held.Count.ShouldBe(1));
        _ = WorkOrderSearchSteps.ChangeAsync(component, WorkOrderSearchPage.Elements.StatusSelect,
            WorkOrderStatus.Assigned.Key);
        await component.WaitForAssertionAsync(() => bus.Held.Count.ShouldBe(2));
    }

    private static async Task AssertNewestWins(
        IRenderedComponent<WorkOrderSearchPage> component,
        NavigationManager navigation)
    {
        await component.WaitForAssertionAsync(() =>
        {
            navigation.Uri.ShouldContain("Creator=");
            navigation.Uri.ShouldContain("Status=");
            component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.WorkOrderLink, "NEWEST1"))
                .Count.ShouldBe(1);
            component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.WorkOrderLink, "STALE01"))
                .Count.ShouldBe(0);
        });
    }
}
