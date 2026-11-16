using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class CommandRoleTests
{
    private static readonly WorkOrder Order = TestWorkOrderBuilder.Create()
        .WithNumber("WO-1")
        .WithStatus(WorkOrderStatus.Draft)
        .Build();

    private static readonly Employee User = EmployeeBuilder.Create("user")
        .WithFirstName("U")
        .WithLastName("Ser")
        .WithEmail("user@example.com")
        .Build();

    [Test]
    public void Role_WhenCommandsShareBeginStatus_HasAtMostOneForward()
    {
        var excess = new StateCommandList()
            .GetAllStateCommands(Order, User)
            .GroupBy(command => command.GetBeginStatus().Key)
            .Where(group => group.Count(command => command.Role == CommandRole.Forward) > 1)
            .Select(group => group.Key)
            .ToArray();

        excess.ShouldBeEmpty();
    }

    [Test]
    public void Role_WhenAssign_IsForward()
    {
        new DraftToAssignedCommand(Order, User).Role.ShouldBe(CommandRole.Forward);
    }

    [Test]
    public void Role_WhenBegin_IsForward()
    {
        new AssignedToInProgressCommand(Order, User).Role.ShouldBe(CommandRole.Forward);
    }

    [Test]
    public void Role_WhenComplete_IsForward()
    {
        new InProgressToCompleteCommand(Order, User).Role.ShouldBe(CommandRole.Forward);
    }

    [Test]
    public void Role_WhenSave_IsSecondary()
    {
        new SaveDraftCommand(Order, User).Role.ShouldBe(CommandRole.Secondary);
    }

    [Test]
    public void Role_WhenShelve_IsSecondary()
    {
        new InProgressToAssignedCommand(Order, User).Role.ShouldBe(CommandRole.Secondary);
    }

    [Test]
    public void Role_WhenWaitOnVendor_IsSecondary()
    {
        new InProgressToWaitingOnVendorCommand(Order, User).Role.ShouldBe(CommandRole.Secondary);
    }

    [Test]
    public void Role_WhenResume_IsSecondary()
    {
        new WaitingOnVendorToInProgressCommand(Order, User).Role.ShouldBe(CommandRole.Secondary);
    }

    [Test]
    public void Role_WhenCancelFromAssigned_IsDestructive()
    {
        new AssignedToCancelledCommand(Order, User).Role.ShouldBe(CommandRole.Destructive);
    }

    [Test]
    public void Role_WhenCancelFromInProgress_IsDestructive()
    {
        new InProgressToCancelledCommand(Order, User).Role.ShouldBe(CommandRole.Destructive);
    }

    [Test]
    public void Role_WhenCancelWhileWaitingOnVendor_IsDestructive()
    {
        new WaitingOnVendorToCancelledCommand(Order, User).Role.ShouldBe(CommandRole.Destructive);
    }
}
