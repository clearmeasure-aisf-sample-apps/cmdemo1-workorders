using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>Filter keys that can appear as removable chips in the "Active" row.</summary>
public enum FilterChipKey
{
    Creator,
    Assignee,
    Status,
    Priority,
    Building,
    Room,
    NoBuilding,
    NoRoom,
    OpenOnly,
    CompletedWithinDays,
    Q
}

/// <summary>One removable chip: which filter it clears and the text it shows.</summary>
public sealed record FilterChip(FilterChipKey Key, string Label);

/// <summary>
/// Builds the "Active" chips for a search state and removes a chip's filter. The "Assigned to me"
/// and "Overdue only" switches are controls of their own and get no chip.
/// </summary>
public static class WorkOrderFilterChips
{
    /// <summary>
    /// One chip per active filter, in display order. <paramref name="employeeName"/> turns a user
    /// name into a full name (it may return the user name when unknown).
    /// </summary>
    public static IReadOnlyList<FilterChip> Build(WorkOrderSearchUrlState state, Func<string, string> employeeName)
    {
        var chips = new List<FilterChip>();
        AddPeople(chips, state, employeeName);
        AddAttributes(chips, state);
        AddFlags(chips, state);
        return chips;
    }

    private static readonly Dictionary<FilterChipKey, Func<WorkOrderSearchUrlState, WorkOrderSearchUrlState>> Removers = new()
    {
        [FilterChipKey.Creator] = s => s with { Creator = null },
        [FilterChipKey.Assignee] = s => s with { Assignee = null },
        [FilterChipKey.Status] = s => s with { Status = null },
        [FilterChipKey.Priority] = s => s with { Priority = null },
        [FilterChipKey.Building] = s => s with { Building = null },
        [FilterChipKey.Room] = s => s with { Room = null },
        [FilterChipKey.NoBuilding] = s => s with { NoBuilding = false },
        [FilterChipKey.NoRoom] = s => s with { NoRoom = false },
        [FilterChipKey.OpenOnly] = s => s with { OpenOnly = false },
        [FilterChipKey.CompletedWithinDays] = s => s with { CompletedWithinDays = null },
        [FilterChipKey.Q] = s => s with { Q = null }
    };

    /// <summary>The state with the chip's filter cleared and paging reset.</summary>
    public static WorkOrderSearchUrlState Remove(WorkOrderSearchUrlState state, FilterChipKey key) =>
        Removers[key](state) with { Page = 1 };

    private static void AddPeople(List<FilterChip> chips, WorkOrderSearchUrlState state, Func<string, string> employeeName)
    {
        AddIf(chips, state.Creator != null, FilterChipKey.Creator, () => $"Creator: {employeeName(state.Creator!)}");
        AddIf(chips, state is { Assignee: not null, AssignedToMe: false }, FilterChipKey.Assignee,
            () => $"Assignee: {employeeName(state.Assignee!)}");
    }

    private static void AddAttributes(List<FilterChip> chips, WorkOrderSearchUrlState state)
    {
        AddIf(chips, state.Status != null, FilterChipKey.Status, () => $"Status: {WorkOrderStatus.FromKey(state.Status!).FriendlyName}");
        AddIf(chips, state.Priority.HasValue, FilterChipKey.Priority, () => $"Priority: {state.Priority}");
        AddIf(chips, state.Building != null, FilterChipKey.Building, () => $"Building: {state.Building}");
        AddIf(chips, state is { NoBuilding: true, Building: null }, FilterChipKey.NoBuilding, () => "Building: No building");
        AddIf(chips, state.Room != null, FilterChipKey.Room, () => $"Room: {state.Room}");
        AddIf(chips, state is { NoRoom: true, Room: null }, FilterChipKey.NoRoom, () => "Room: No room");
    }

    private static void AddFlags(List<FilterChip> chips, WorkOrderSearchUrlState state)
    {
        AddIf(chips, state.OpenOnly, FilterChipKey.OpenOnly, () => "Not closed");
        AddIf(chips, state.CompletedWithinDays.HasValue, FilterChipKey.CompletedWithinDays,
            () => $"Completed in last {state.CompletedWithinDays} days");
        AddIf(chips, state.Q != null, FilterChipKey.Q, () => $"Search: {state.Q}");
    }

    private static void AddIf(List<FilterChip> chips, bool condition, FilterChipKey key, Func<string> label)
    {
        if (condition)
        {
            chips.Add(new FilterChip(key, label()));
        }
    }
}
