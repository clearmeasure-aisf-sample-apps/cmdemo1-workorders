using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToWaitingOnVendorCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Wait on vendor";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Waiting on vendor";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.WaitingOnVendor;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }
}
