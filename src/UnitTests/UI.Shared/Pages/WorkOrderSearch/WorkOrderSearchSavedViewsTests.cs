using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

public class WorkOrderSearchSavedViewsTests
{
    [Test]
    public async Task SavedViewsMenu_TogglesAndClosesOnEscape()
    {
        await using var ctx = WorkOrderSearchPageBuilder.Create().Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);

        component.Find("#saved-views-menu").HasAttribute("hidden").ShouldBeFalse();
        component.FindAll("#saved-views-menu a[role='menuitem']").Select(a => a.GetAttribute("href"))
            .ShouldContain("workorder/search?OverdueOnly=true");

        await WorkOrderSearchSteps.PressEscapeOnMenuAsync(component, ".menu-anchor");

        component.Find("#saved-views-menu").HasAttribute("hidden").ShouldBeTrue();
    }

    [Test]
    public async Task SavedViewsMenu_ListsBuiltInViewsThenUserViews()
    {
        var bus = new StubBus();
        bus.SavedViews.Add(new SavedSearchView { Name = "High priority", Query = "Priority=High" });
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);

        var links = component.FindAll("#saved-views-menu a[role='menuitem']");
        links.Select(a => a.TextContent.Trim()).ShouldBe(
            ["Open work orders", "Assigned to me", "Overdue", "Completed in last 30 days", "High priority"]);
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.UserSavedViewLink, "High priority"))
            .GetAttribute("href").ShouldBe("workorder/search?Priority=High");
        bus.SummaryRequestCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task SaveCurrentView_SendsCommandWithCurrentQueryString()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo("workorder/search?Priority=High&OverdueOnly=true");
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        await WorkOrderSearchSteps.ClickSaveCurrentViewAsync(component);
        await WorkOrderSearchSteps.InputSavedViewNameAsync(component, "  Urgent  ");
        await WorkOrderSearchSteps.SubmitSaveViewFormAsync(component);

        AssertSavedUrgentView(bus, component);
    }

    private static void AssertSavedUrgentView(StubBus bus, IRenderedComponent<WorkOrderSearchPage> component)
    {
        bus.LastSaveCommand.ShouldNotBeNull();
        bus.LastSaveCommand.UserName.ShouldBe(WorkOrderSearchPageBuilder.DefaultUserName);
        bus.LastSaveCommand.Name.ShouldBe("Urgent");
        bus.LastSaveCommand.Query.ShouldBe("Priority=High&OverdueOnly=true");
        component.FindAll("form.save-view-form").Count.ShouldBe(0);
        component.Find("#saved-views-menu").HasAttribute("hidden").ShouldBeTrue();
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.UserSavedViewLink, "Urgent"))
            .Count.ShouldBe(1);
    }

    [Test]
    public async Task SaveCurrentView_EmptyName_ShowsAlertAndSendsNothing()
    {
        var bus = new StubBus();
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        await WorkOrderSearchSteps.ClickSaveCurrentViewAsync(component);
        await WorkOrderSearchSteps.SubmitSaveViewFormAsync(component);

        var alert = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SavedViewError));
        alert.GetAttribute("role").ShouldBe("alert");
        alert.TextContent.ShouldBe("Enter a name for the view.");
        bus.LastSaveCommand.ShouldBeNull();
    }

    [Test]
    public async Task SaveCurrentView_ServerError_IsDisplayed()
    {
        var bus = new StubBus { SaveResult = SavedSearchViewResult.Failure("A view named 'Mine' already exists.") };
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        await WorkOrderSearchSteps.ClickSaveCurrentViewAsync(component);
        await WorkOrderSearchSteps.InputSavedViewNameAsync(component, "Mine");
        await WorkOrderSearchSteps.SubmitSaveViewFormAsync(component);

        bus.LastSaveCommand.ShouldNotBeNull();
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SavedViewError)).TextContent
            .ShouldBe("A view named 'Mine' already exists.");
        component.FindAll("form.save-view-form").Count.ShouldBe(1);
    }

    [Test]
    public async Task SaveCurrentView_WhenBusThrows_ShowsErrorAndKeepsPage()
    {
        var bus = new StubBus { ThrowOnSaveCommand = true };
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        await WorkOrderSearchSteps.ClickSaveCurrentViewAsync(component);
        await WorkOrderSearchSteps.InputSavedViewNameAsync(component, "Mine");
        await WorkOrderSearchSteps.SubmitSaveViewFormAsync(component);

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SavedViewError)).TextContent
            .ShouldContain("could not be saved");
        component.FindAll("form.save-view-form").Count.ShouldBe(1);
    }

    [Test]
    public async Task DeleteSavedView_WhenBusThrows_ShowsError()
    {
        var bus = new StubBus { ThrowOnDeleteCommand = true };
        bus.SavedViews.Add(new SavedSearchView { Name = "Old", Query = "OverdueOnly=true" });
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.UserSavedViewDeleteButton, "Old");

        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SavedViewDeleteError)).TextContent
            .ShouldContain("could not be deleted");
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.UserSavedViewLink, "Old"))
            .Count.ShouldBe(1);
    }

    [Test]
    public async Task DeleteSavedView_SendsCommandWithViewId()
    {
        var bus = new StubBus();
        var view = new SavedSearchView { Name = "Old", Query = "OverdueOnly=true" };
        bus.SavedViews.Add(view);
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        await WorkOrderSearchSteps.ClickTestIdAsync(component, WorkOrderSearchPage.Elements.UserSavedViewDeleteButton, "Old");

        bus.LastDeleteCommand.ShouldNotBeNull();
        bus.LastDeleteCommand.Id.ShouldBe(view.Id);
        bus.LastDeleteCommand.UserName.ShouldBe(WorkOrderSearchPageBuilder.DefaultUserName);
        component.FindAll(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.UserSavedViewLink, "Old"))
            .Count.ShouldBe(0);
    }

    [Test]
    public async Task SavedViewsMenu_IsKeyboardReachable_AndEscapeClosesSaveForm()
    {
        var bus = new StubBus();
        bus.SavedViews.Add(new SavedSearchView { Name = "Mine", Query = "Priority=Low" });
        await using var ctx = WorkOrderSearchPageBuilder.Create().WithBus(bus).Build();
        var component = ctx.Render<WorkOrderSearchPage>();

        await WorkOrderSearchSteps.OpenSavedViewsMenuAsync(component);
        AssertKeyboardMenuItems(component);

        await WorkOrderSearchSteps.ClickSaveCurrentViewAsync(component);
        AssertSaveFormLabels(component);
        await WorkOrderSearchSteps.PressEscapeOnMenuAsync(component, ".menu-anchor");

        component.FindAll("form.save-view-form").Count.ShouldBe(0);
        component.Find("#saved-views-menu").HasAttribute("hidden").ShouldBeTrue();
    }

    private static void AssertKeyboardMenuItems(IRenderedComponent<WorkOrderSearchPage> component)
    {
        var items = component.FindAll("#saved-views-menu [role='menuitem']");
        items.Count.ShouldBe(7);
        items.ShouldAllBe(i => i.TagName == "A" || i.TagName == "BUTTON");
        component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.UserSavedViewDeleteButton, "Mine"))
            .GetAttribute("aria-label").ShouldBe("Delete saved view Mine");
    }

    private static void AssertSaveFormLabels(IRenderedComponent<WorkOrderSearchPage> component)
    {
        var input = component.Find(WorkOrderSearchSteps.TestId(WorkOrderSearchPage.Elements.SavedViewNameInput));
        component.Find($"label[for='{input.Id}']").TextContent.ShouldBe("View name");
        input.GetAttribute("maxlength").ShouldBe("100");
    }
}
