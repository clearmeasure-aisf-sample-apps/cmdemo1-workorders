using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public abstract record StateCommandBase(WorkOrder WorkOrder, Employee CurrentUser) : IStateCommand
{
    public abstract WorkOrderStatus GetBeginStatus();
    protected abstract WorkOrderStatus GetEndStatus();
    protected abstract bool UserCanExecute(Employee currentUser);
    public abstract string TransitionVerbPresentTense { get; }
    public abstract string TransitionVerbPastTense { get; }

    public IStateTransitionEvent? StateTransitionEvent { get; protected set; }

    public bool IsValid()
    {
        var beginStatusMatches = WorkOrder.Status == GetBeginStatus();
        var currentUserIsCorrectRole = UserCanExecute(CurrentUser);
        return beginStatusMatches && currentUserIsCorrectRole && IsAllowedInCurrentState();
    }

    /// <summary>
    /// Extra precondition beyond the begin status and the user's role (for example a sub-state of
    /// In Progress). Defaults to <c>true</c>.
    /// </summary>
    protected virtual bool IsAllowedInCurrentState() => true;

    public bool Matches(string commandName)
    {
        return TransitionVerbPresentTense == commandName;
    }

    public virtual void Execute(StateCommandContext context)
    {
        WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
    }
}