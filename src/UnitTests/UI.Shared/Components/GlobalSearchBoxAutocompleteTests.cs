using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class GlobalSearchBoxAutocompleteTests
{
    [Test]
    public async Task ShouldDisableBrowserAutocomplete_OnTheSearchInput()
    {
        await using var ctx = ComponentContextBuilder.WithStubBuses();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var input = ctx.Render<GlobalSearchBox>()
            .Find($"[data-testid='{GlobalSearchBox.Elements.GlobalSearchInput}']");
        input.GetAttribute("autocomplete").ShouldBe("off");
    }
}
