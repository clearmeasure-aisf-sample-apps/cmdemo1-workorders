using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// One state change applied to every selected work order: the command named <see cref="Verb"/>, run by
/// <see cref="CurrentUser"/>, after <see cref="Prepare"/> sets what the command needs (for example the assignee).
/// </summary>
public sealed record BulkStateChange(string Verb, Employee CurrentUser, Action<WorkOrder> Prepare)
{
    /// <summary>Prepares the work order and returns the valid command for the verb, or <c>null</c> when none applies.</summary>
    public IStateCommand? CommandFor(WorkOrder workOrder)
    {
        Prepare(workOrder);
        return new StateCommandList().GetValidStateCommands(workOrder, CurrentUser)
            .FirstOrDefault(candidate => candidate.Matches(Verb));
    }
}
