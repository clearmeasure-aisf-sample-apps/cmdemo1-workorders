# Work Order Completion and Cancellation

**Complete** and **Cancelled** are terminal work-order statuses. A work order cannot leave either status.

## Completing a work order

The assignee can complete a work order while it is **InProgress**. The completion command changes its status to **Complete** and sets `CompletedDate` to the command's current date and time.

## Cancelling a work order

The creator can cancel a work order while it is **Assigned** or **InProgress**. Cancellation is available both while work is underway and while it is waiting on a vendor. The command changes the status to **Cancelled**; it does not set `CompletedDate`.
