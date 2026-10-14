using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record WaitingOnVendorToInProgressCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Resume";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Resumed";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.WaitingOnVendor;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }
}
