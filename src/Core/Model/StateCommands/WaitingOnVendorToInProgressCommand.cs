using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// The assignee resumes an In Progress work order that was waiting on a vendor. The status stays
/// In Progress; <see cref="WorkOrder.WaitingOnVendorSince"/> is cleared.
/// </summary>
public record WaitingOnVendorToInProgressCommand(WorkOrder WorkOrder, Employee CurrentUser)
: StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Resume";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }

    protected override bool IsAllowedInCurrentState() => WorkOrder.WaitingOnVendorSince != null;

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.WaitingOnVendorSince = null;
        base.Execute(context);
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Resumed";
}
