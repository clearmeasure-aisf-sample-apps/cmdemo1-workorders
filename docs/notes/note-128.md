# Note 128

InProgressToCompleteCommand.Name is the string constant "Complete" (`public const string Name = "Complete";`). TransitionVerbPastTense is "Completed". GetBeginStatus returns WorkOrderStatus.InProgress and GetEndStatus returns WorkOrderStatus.Complete. UserCanExecute returns true only when the current user is the work order's assignee. Execute sets WorkOrder.CompletedDate to the context's current date and time, then calls base.Execute. The command is InProgressToCompleteCommand in `src/Core/Model/StateCommands/InProgressToCompleteCommand.cs`.
