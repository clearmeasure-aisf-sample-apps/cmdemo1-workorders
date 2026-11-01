# Note 132

InProgressToAssignedCommand.Name is the string constant "Shelve" (`public const string Name = "Shelve";`). TransitionVerbPastTense is "Shelved". GetBeginStatus returns WorkOrderStatus.InProgress and GetEndStatus returns WorkOrderStatus.Assigned. UserCanExecute returns true only when the current user is the work order's assignee. TransitionVerbPresentTense returns Name. The command is InProgressToAssignedCommand in `src/Core/Model/StateCommands/InProgressToAssignedCommand.cs`.
