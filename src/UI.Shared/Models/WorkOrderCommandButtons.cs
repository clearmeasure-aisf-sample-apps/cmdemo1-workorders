using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// The command buttons of the manage page for the commands valid right now: secondary first, then danger, the
/// primary command last; and whether the shelve reason field is needed.
/// </summary>
public sealed class WorkOrderCommandButtons
{
    public WorkOrderCommandButtons(IEnumerable<IStateCommand> validCommands)
    {
        var commands = validCommands.ToList();
        Sorted = commands.OrderBy(command => RoleRank(command.Role)).ToList();
        ShowsShelveReason = commands.Exists(command => command.Matches(InProgressToAssignedCommand.Name));
    }

    public static WorkOrderCommandButtons None { get; } = new([]);

    public IReadOnlyList<IStateCommand> Sorted { get; }

    public bool Any => Sorted.Count > 0;

    public bool ShowsShelveReason { get; }

    public string CssClass(IStateCommand command) => command.Role switch
    {
        CommandRole.Forward => "btn-app btn-app--primary",
        CommandRole.Destructive => "btn-app btn-app--danger",
        _ => "btn-app btn-app--secondary"
    };

    private static int RoleRank(CommandRole role) => role switch
    {
        CommandRole.Destructive => 1,
        CommandRole.Forward => 2,
        _ => 0
    };
}
