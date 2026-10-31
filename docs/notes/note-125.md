# Note 125

AssignedToCancelledCommand.Name is the string constant "Cancel" (`public const string Name = "Cancel";`). TransitionVerbPastTense is "Cancelled". GetBeginStatus returns WorkOrderStatus.Assigned and GetEndStatus returns WorkOrderStatus.Cancelled. UserCanExecute returns true only when the current user is the work order's creator. Execute sets AssignedDate and Assignee to null, then calls base.Execute. The command is AssignedToCancelledCommand in `src/Core/Model/StateCommands/AssignedToCancelledCommand.cs`.
