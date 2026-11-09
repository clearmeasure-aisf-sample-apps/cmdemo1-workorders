using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class WorkOrderCommandRequestTests
{
    [TestCase("DraftToAssignedCommand", true)]
    [TestCase("AssignedToInProgressCommand", false)]
    public void ShouldAssignTheWorkOrder_OnlyWhenTheCommandIsDraftToAssigned(string commandName, bool expected)
    {
        var request = new WorkOrderCommandRequest("WO-1", commandName, "jsmith", "bwayne", null);

        var assigns = request.AssignsTheWorkOrder;

        assigns.ShouldBe(expected);
    }

    [Test]
    public void ShouldCarryTheShelveReason_WhenTheCommandShelves()
    {
        var request = new WorkOrderCommandRequest("WO-1", "Shelve", "jsmith", null, " Parts ordered ");

        var reason = request.ShelveReason;

        reason.Text.ShouldBe("Parts ordered");
    }
}
