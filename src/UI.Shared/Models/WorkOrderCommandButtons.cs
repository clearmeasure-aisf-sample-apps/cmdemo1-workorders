using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// The command buttons of the manage page for the commands valid right now: secondary first, then danger, the
/// primary command last; and whether the shelve reason field is needed.
/// </summary>
public sealed class WorkOrderCommandButtons
{
    private readonly List<string> _verbs;

    public WorkOrderCommandButtons(IEnumerable<IStateCommand> validCommands)
    {
        var commands = validCommands.ToList();
        _verbs = commands.Select(command => command.TransitionVerbPresentTense).ToList();
        Sorted = commands.OrderBy(command => StyleRank(Style(command))).ToList();
    }

    public static WorkOrderCommandButtons None { get; } = new([]);

    public IReadOnlyList<IStateCommand> Sorted { get; }

    public bool Any => _verbs.Count > 0;

    public bool ShowsShelveReason => _verbs.Contains(InProgressToAssignedCommand.Name);

    public string CssClass(IStateCommand command) => Style(command) switch
    {
        "btn-primary" => "btn-app btn-app--primary",
        "btn-outline-danger" => "btn-app btn-app--danger",
        _ => "btn-app btn-app--secondary"
    };

    private string Style(IStateCommand command) =>
        CommandButtonStyleHelper.GetButtonStyle(command.TransitionVerbPresentTense, _verbs);

    private static int StyleRank(string style) => style switch
    {
        "btn-outline-danger" => 1,
        "btn-primary" => 2,
        _ => 0
    };
}
