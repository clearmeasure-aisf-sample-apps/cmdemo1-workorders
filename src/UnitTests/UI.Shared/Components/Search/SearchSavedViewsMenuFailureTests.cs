using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchSavedViewsMenuFailureTests
{
    [Test]
    public async Task ShouldListNoUserViews_WhenLoadFails()
    {
        var bus = new StubBus { FailSavedViewsQuery = true };
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);

        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters
            .Add(p => p.CurrentUserName, "ada")
            .Add(p => p.CurrentQuery, string.Empty));

        menu.FindAll($"[data-testid^='{WorkOrderSearch.Elements.UserSavedViewLink}']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldShowSaveFailureMessage_WhenSaveThrows()
    {
        var bus = new StubBus { ThrowOnSaveCommand = true };
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);
        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters
            .Add(p => p.CurrentUserName, "ada")
            .Add(p => p.CurrentQuery, "Status=Assigned"));
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SaveCurrentViewButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewNameInput}']")
            .InputAsync(new ChangeEventArgs { Value = "My assigned" });

        await menu.Find("form.save-view-form").SubmitAsync();

        menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewError}']").TextContent
            .ShouldBe("The view could not be saved. Please try again later.");
    }

    [Test]
    public async Task ShouldShowDeleteFailureMessage_WhenDeleteThrows()
    {
        var view = new SavedSearchView { Name = "Boiler room", Query = "Room=B1" };
        var bus = new StubBus { ThrowOnDeleteCommand = true };
        bus.SavedViews.Add(view);
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);
        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters
            .Add(p => p.CurrentUserName, "ada")
            .Add(p => p.CurrentQuery, string.Empty));
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']").ClickAsync(new());

        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.UserSavedViewDeleteButton}Boiler room']")
            .ClickAsync(new());

        menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewDeleteError}']").TextContent
            .ShouldBe("The view could not be deleted. Please try again later.");
    }
}
