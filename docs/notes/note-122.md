# Note 122

SaveDraftCommand.Execute sets WorkOrder.CreatedDate to the context's current date and time only when CreatedDate is null (`if (WorkOrder.CreatedDate.Equals(null)) { WorkOrder.CreatedDate = context.CurrentDateTime; }`), then calls base.Execute. The method is `SaveDraftCommand.Execute` in `src/Core/Model/StateCommands/SaveDraftCommand.cs`.
