using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchSavedViewsMenuNameInputTests
{
    [Test]
    public async Task ShouldTurnOffAutofillOnTheNameInput_WhenTheSaveFormIsOpen()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters.Add(p => p.CurrentQuery, "Status=Assigned"));
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SaveCurrentViewButton}']").ClickAsync(new());

        menu.Find("#saved-view-name").GetAttribute("autocomplete").ShouldBe("off");
    }
}
