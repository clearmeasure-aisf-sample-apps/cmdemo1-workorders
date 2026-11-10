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
/// Applies the existing state commands to several work orders through <see cref="IBus"/>.
/// The targeted rows are loaded in one query; each valid command is then sent on its own.
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
        return await ApplyAsync(numbers, new BulkStateChange(DraftToAssignedCommand.Name, currentUser, order => order.Assignee = assignee));
    }

    /// <summary>Sends the command named <paramref name="verb"/> (for example "Begin") to each work order.</summary>
    public Task<BulkActionOutcome> ChangeStatusAsync(IEnumerable<string> numbers, string verb, Employee currentUser) =>
        ApplyAsync(numbers, new BulkStateChange(verb, currentUser, _ => { }));

    private async Task<BulkActionOutcome> ApplyAsync(IEnumerable<string> numbers, BulkStateChange change)
    {
        var requested = numbers.ToArray();
        var byNumber = await LoadByNumberAsync(requested);
        return await ApplyLoadedAsync(requested, byNumber, change);
    }

    private async Task<Dictionary<string, WorkOrder>> LoadByNumberAsync(string[] requested)
    {
        if (requested.Length == 0)
        {
            return new Dictionary<string, WorkOrder>(StringComparer.Ordinal);
        }

        var loaded = await bus.Send(new WorkOrdersByNumbersQuery(requested));
        return loaded
            .Where(order => !string.IsNullOrEmpty(order.Number))
            .ToDictionary(order => order.Number!, StringComparer.Ordinal);
    }

    private async Task<BulkActionOutcome> ApplyLoadedAsync(string[] requested, Dictionary<string, WorkOrder> byNumber,
        BulkStateChange change)
    {
        var updated = 0;
        var skipped = 0;
        foreach (var number in requested)
        {
            if (!TryGetOrder(byNumber, number, out var order))
            {
                skipped++;
                continue;
            }

            // The read is one query. Each state command is still its own send.
#pragma warning disable CM0001
            var applied = await ApplyOneAsync(order, change);
#pragma warning restore CM0001
            if (applied)
            {
                updated++;
                continue;
            }

            skipped++;
        }

        return new BulkActionOutcome(updated, skipped);
    }

    private static bool TryGetOrder(Dictionary<string, WorkOrder> byNumber, string number, out WorkOrder order)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            order = null!;
            return false;
        }

        return byNumber.TryGetValue(number, out order!);
    }

    private async Task<bool> ApplyOneAsync(WorkOrder order, BulkStateChange change)
    {
        var command = change.CommandFor(order);
        if (command == null)
        {
            return false;
        }

        var result = await bus.Send(command);
        uiBus.Notify(new WorkOrderChangedEvent(result));
        return true;
    }
}
