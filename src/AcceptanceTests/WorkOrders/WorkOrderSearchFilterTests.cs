using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchFilterTests : AcceptanceTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await LoginAsCurrentUser();
    }

    [Test, Retry(2)]
    public async Task ShouldFilterWorkOrdersByRoomUsingUrlParameter()
    {
        // Arrange & Act
        await Page.GotoAsync("workorder/search?Building=Sanctuary&Room=101");

        // Assert
        await Expect(Page).ToHaveTitleAsync(new Regex("Search"));
    }

    [Test, Retry(2)]
    public async Task ShouldFilterWorkOrdersWithNoRoomUsingUrlParameter()
    {
        // Arrange & Act
        await Page.GotoAsync("workorder/search?NoRoom=true");

        // Assert
        await Expect(Page).ToHaveTitleAsync(new Regex("Search"));
    }
}
