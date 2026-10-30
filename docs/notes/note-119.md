# Note 119

StateCommandBase.Execute calls WorkOrder.ChangeStatus with the current user, the context's current date and time, and the command's end status (`WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus())`). The method is `StateCommandBase.Execute` in `src/Core/Model/StateCommands/StateCommandBase.cs`.
