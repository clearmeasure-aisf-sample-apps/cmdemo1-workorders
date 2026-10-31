# Note 50

A newly chosen due date earlier than today is refused. WorkOrder.DueDateInThePastMessage is "The due date cannot be in the past." WorkOrder.IsNewlySetPastDueDate is true only when the due date is not null, differs from the previous due date, and is earlier than today. SaveDraftCommandValidator and WorkOrderManageModel apply that rule with today in America/Chicago and report that message.
