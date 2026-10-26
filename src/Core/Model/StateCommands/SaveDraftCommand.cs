using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record SaveDraftCommand(WorkOrder WorkOrder, Employee CurrentUser) :
StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Save";

    /// <summary>
    /// The room number persisted on the work order before this command was created.
    /// Used to skip entry-length validation when the room number is unchanged.
    /// </summary>
    public string? PreviousRoomNumber { get; init; }

    /// <summary>
    /// The due date persisted on the work order before this command was created.
    /// An unchanged value is allowed even when it is earlier than today.
    /// </summary>
    public DateOnly? PreviousDueDate { get; init; }

    /// <summary>Returns <paramref name="command"/> with <see cref="PreviousRoomNumber"/> set when it is a Save command; any other command unchanged.</summary>
    public static IStateCommand WithPreviousRoomNumber(IStateCommand command, string? previousRoomNumber) =>
        command is SaveDraftCommand save ? save with { PreviousRoomNumber = previousRoomNumber } : command;

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Draft;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Draft;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Saved";

    public override void Execute(StateCommandContext context)
    {
        if (WorkOrder.CreatedDate.Equals(null))
        {
            WorkOrder.CreatedDate = context.CurrentDateTime;
        }

        base.Execute(context);
    }
}