# Work Order Assignment

The work-order manage form offers all employees as assignee options; the assignment transition itself does not require the selected employee to have a particular role. The separate Teams & Vendors page lists only employees with at least one role whose `CanFulfillWorkOrder` permission is true.

`WorkOrder.CanReassign()` returns true only while the work order is in Draft. The manage form disables the assignee selector for an existing work order in any other status. Assigning a Draft work order is a transition that can be executed by its creator.

When a Draft work order moves to Assigned, `AssignedDate` is set to the transition's current date and time. Work orders created directly in Assigned status by dated-work-order creation also receive an `AssignedDate` at creation. The assigned date appears in the manage form and on the printed work order. Cancelling an assigned work order clears both the assignee and assigned date.
