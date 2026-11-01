using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// The assignee marks an In Progress work order as waiting on a vendor. The status stays In Progress;
/// only <see cref="WorkOrder.WaitingOnVendorSince"/> is set.
/// </summary>
public record InProgressToWaitingOnVendorCommand(WorkOrder WorkOrder, Employee CurrentUser)
: StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Wait on vendor";

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

    protected override bool IsAllowedInCurrentState() => WorkOrder.WaitingOnVendorSince == null;

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.WaitingOnVendorSince = context.CurrentDateTime;
        base.Execute(context);
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Waiting on vendor";
}
