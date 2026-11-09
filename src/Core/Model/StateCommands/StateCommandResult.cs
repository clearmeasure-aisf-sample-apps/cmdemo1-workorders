// ReSharper disable NotAccessedPositionalProperty.Global -- positional members are the command result returned to callers
// ReSharper disable NotAccessedPositionalProperty.Local -- positional members are the command result returned to callers
namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record StateCommandResult(
    WorkOrder WorkOrder,
    string TransitionVerbPresentTense = "Save",
    string DebugMessage = "");
