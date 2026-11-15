using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchSavedViewsMenuFormTests
{
    [Test]
    public async Task ShouldNameTheSaveForm_WhenTheSaveFormIsOpen()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        var menu = ctx.Render<SearchSavedViewsMenu>(parameters => parameters.Add(p => p.CurrentQuery, "Status=Assigned"));
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SavedViewsButton}']").ClickAsync(new());
        await menu.Find($"[data-testid='{WorkOrderSearch.Elements.SaveCurrentViewButton}']").ClickAsync(new());

        menu.Find("form.save-view-form").GetAttribute("aria-label").ShouldBe("Save current view");
    }
}
