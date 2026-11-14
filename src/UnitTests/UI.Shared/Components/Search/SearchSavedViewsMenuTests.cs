using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchSavedViewsMenuTests
{
    [Test]
    public async Task ShouldListTheSignedInUsersViews_WhenRendered()
    {
        var bus = new StubBus();
        bus.SavedViews.Add(new SavedSearchView { Name = "Boiler room", Query = "Room=B1" });
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);

        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters
            .Add(p => p.CurrentUserName, "ada")
            .Add(p => p.CurrentQuery, string.Empty));

        menu.Find($"[data-testid='{WorkOrderSearch.Elements.UserSavedViewLink}Boiler room']").GetAttribute("href").ShouldEndWith("Room=B1");
    }

    [Test]
    public async Task ShouldNameTheViewInTheDeleteTooltip_WhenAUserViewIsListed()
    {
        var bus = new StubBus();
        bus.SavedViews.Add(new SavedSearchView { Name = "My open orders", Query = "OpenOnly=true" });
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);

        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters
            .Add(p => p.CurrentUserName, "ada")
            .Add(p => p.CurrentQuery, string.Empty));

        var deleteButton = menu.Find($"[data-testid='{WorkOrderSearch.Elements.UserSavedViewDeleteButton}My open orders']");
        deleteButton.GetAttribute("title").ShouldBe("Delete saved view My open orders");
        deleteButton.GetAttribute("aria-label").ShouldBe("Delete saved view My open orders");
    }

    [Test]
    public async Task ShouldSaveTheCurrentQueryUnderTheTypedName_WhenSaved()
    {
        var bus = new StubBus();
        await using var ctx = ComponentContextBuilder.WithStubBuses(bus);
        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters
            .Add(p => p.CurrentUserName, "ada")
            .Add(p => p.CurrentQuery, "Status=Assigned"));
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SaveCurrentViewButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewNameInput}']").InputAsync(new ChangeEventArgs { Value = "My assigned" });

        await menu.Find("form.save-view-form").SubmitAsync();

        bus.LastSaveCommand.ShouldBe(new SaveSearchViewCommand("ada", "My assigned", "Status=Assigned"));
    }

    [Test]
    public async Task ShouldAskToSignIn_WhenSavingWithoutAUser()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters.Add(p => p.CurrentQuery, "Status=Assigned"));
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SaveCurrentViewButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewNameInput}']").InputAsync(new ChangeEventArgs { Value = "My assigned" });

        await menu.Find("form.save-view-form").SubmitAsync();

        menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewError}']").TextContent.ShouldBe("Sign in to save a view.");
    }
}
