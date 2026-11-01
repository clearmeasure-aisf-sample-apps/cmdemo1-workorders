namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToAssignedCommand(WorkOrder WorkOrder, Employee CurrentUser)
: StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Shelve";

    /// <summary>Maximum length of <see cref="Reason"/>.</summary>
    public const int ReasonMaxLength = 200;

    /// <summary>
    /// Why the work order is shelved. Required when the command is sent, at most
    /// <see cref="ReasonMaxLength"/> characters after trimming.
    /// </summary>
    public string? Reason { get; init; }

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Shelved";
}