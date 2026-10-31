# Note 144

SaveDraftCommand.Name is the string constant "Save" (`public const string Name = "Save";`). GetBeginStatus returns WorkOrderStatus.Draft and GetEndStatus returns WorkOrderStatus.Draft. TransitionVerbPresentTense returns Name. TransitionVerbPastTense is "Saved". UserCanExecute returns true only when the current user is the work order's creator (`return currentUser == WorkOrder.Creator;`). The command is SaveDraftCommand in `src/Core/Model/StateCommands/SaveDraftCommand.cs`.
