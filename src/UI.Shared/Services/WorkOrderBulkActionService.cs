using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>How a bulk action went: rows updated and rows skipped because the command was not valid.</summary>
public sealed record BulkActionOutcome(int Updated, int Skipped)
{
    /// <summary>"3 updated, 1 skipped".</summary>
    public string Text => $"{Updated} updated, {Skipped} skipped";
}

/// <summary>
/// Applies the existing state commands to several work orders, one at a time through <see cref="IBus"/>.
/// Domain rules are unchanged: a row where the command is not valid for its state or the current user
/// is skipped and counted.
/// </summary>
public class WorkOrderBulkActionService(IBus bus, IUiBus uiBus)
{
    private static readonly HashSet<string> ExcludedFromChangeStatus =
        [SaveDraftCommand.Name, DraftToAssignedCommand.Name, InProgressToAssignedCommand.Name];

    /// <summary>
    /// The status changes offered for the selection: every verb valid for at least one of
    /// <paramref name="workOrders"/>, except Save, Assign (needs an assignee) and Shelve (needs a reason).
    /// </summary>
    public static IReadOnlyList<string> ChangeStatusOptions(IEnumerable<WorkOrder> workOrders, Employee currentUser) =>
        workOrders
            .SelectMany(order => new StateCommandList().GetValidStateCommands(order, currentUser))
            .Select(command => command.TransitionVerbPresentTense)
            .Where(verb => !ExcludedFromChangeStatus.Contains(verb))
            .Distinct()
            .ToArray();

    /// <summary>Assigns each work order to <paramref name="assigneeUserName"/> with the "Assign" command.</summary>
    public async Task<BulkActionOutcome> AssignAsync(IEnumerable<string> numbers, string assigneeUserName, Employee currentUser)
    {
        var assignee = await bus.Send(new EmployeeByUserNameQuery(assigneeUserName));
        return await ApplyAsync(numbers, DraftToAssignedCommand.Name, currentUser, order => order.Assignee = assignee);
    }

    /// <summary>Sends the command named <paramref name="verb"/> (for example "Begin") to each work order.</summary>
    public Task<BulkActionOutcome> ChangeStatusAsync(IEnumerable<string> numbers, string verb, Employee currentUser) =>
        ApplyAsync(numbers, verb, currentUser, _ => { });

    private async Task<BulkActionOutcome> ApplyAsync(IEnumerable<string> numbers, string verb, Employee currentUser,
        Action<WorkOrder> prepare)
    {
        var updated = 0;
        var skipped = 0;
        foreach (var number in numbers)
        {
            var applied = await ApplyOneAsync(number, verb, currentUser, prepare);
            updated += applied ? 1 : 0;
            skipped += applied ? 0 : 1;
        }

        return new BulkActionOutcome(updated, skipped);
    }

    private async Task<bool> ApplyOneAsync(string number, string verb, Employee currentUser, Action<WorkOrder> prepare)
    {
        var order = await bus.Send(new WorkOrderByNumberQuery(number));
        if (order == null)
        {
            return false;
        }

        prepare(order);
        var command = new StateCommandList().GetValidStateCommands(order, currentUser)
            .FirstOrDefault(candidate => candidate.Matches(verb));
        if (command == null)
        {
            return false;
        }

        var result = await bus.Send(command);
        uiBus.Notify(new WorkOrderChangedEvent(result));
        return true;
    }
}
