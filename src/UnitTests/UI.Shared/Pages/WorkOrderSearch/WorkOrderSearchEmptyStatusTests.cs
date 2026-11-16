using Bunit;
using Shouldly;
using SearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

[TestFixture]
public class WorkOrderSearchEmptyStatusTests
{
    [Test]
    public async Task EmptyResults_AreAnnouncedWithRoleStatus()
    {
        await using var context = WorkOrderSearchPageBuilder.Create().WithOrders().Build();

        var component = context.Render<SearchPage>();

        component.Find(".empty-results").GetAttribute("role").ShouldBe("status");
    }
}
