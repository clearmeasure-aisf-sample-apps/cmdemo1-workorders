# Note 124

AssignedToInProgressCommand.Name is the string constant "Begin" (`public const string Name = "Begin";`). TransitionVerbPastTense is "Begun". GetBeginStatus returns WorkOrderStatus.Assigned and GetEndStatus returns WorkOrderStatus.InProgress. UserCanExecute returns true only when the current user is the work order's assignee. The command is AssignedToInProgressCommand in `src/Core/Model/StateCommands/AssignedToInProgressCommand.cs`.
